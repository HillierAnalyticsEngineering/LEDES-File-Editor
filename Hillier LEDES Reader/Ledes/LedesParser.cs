using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

namespace Hillier_LEDES_Reader.Ledes
{
    /// <summary>Reads any supported LEDES format into a <see cref="LedesDocument"/>.</summary>
    public static class LedesParser
    {
        /// <summary>Largest file the editor will open. LEDES invoices are rarely more than a few MB.</summary>
        public const long MaxFileBytes = 200L * 1024 * 1024;

        public static LedesDocument Load(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("No file was chosen.", "path");
            }

            FileInfo info = new FileInfo(path);
            if (!info.Exists)
            {
                throw new FileNotFoundException("The file no longer exists.", path);
            }

            if (info.Length == 0)
            {
                throw new LedesFormatException("The file is empty.");
            }

            if (info.Length > MaxFileBytes)
            {
                throw new LedesFormatException("The file is " + (info.Length / (1024 * 1024)) +
                    " MB. The editor opens files up to " + (MaxFileBytes / (1024 * 1024)) + " MB.");
            }

            byte[] bytes;
            // FileShare.ReadWrite lets us read a file another program (such as the billing system) still has open.
            using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                bytes = new byte[fs.Length];
                int read = 0;
                while (read < bytes.Length)
                {
                    int n = fs.Read(bytes, read, bytes.Length - read);
                    if (n == 0)
                    {
                        break;
                    }

                    read += n;
                }
            }

            LedesDocument doc = Parse(DecodeText(bytes));
            doc.FilePath = path;
            return doc;
        }

        /// <summary>Decodes file bytes. Honors a byte-order mark, then tries UTF-8, then falls back to Windows-1252.</summary>
        public static string DecodeText(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0)
            {
                return string.Empty;
            }

            if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
            {
                return Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3);
            }

            if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE)
            {
                return Encoding.Unicode.GetString(bytes, 2, bytes.Length - 2);
            }

            if (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF)
            {
                return Encoding.BigEndianUnicode.GetString(bytes, 2, bytes.Length - 2);
            }

            try
            {
                return new UTF8Encoding(false, true).GetString(bytes);
            }
            catch (DecoderFallbackException)
            {
                return Encoding.GetEncoding(1252).GetString(bytes);
            }
        }

        /// <summary>Parses LEDES text. Throws <see cref="LedesFormatException"/> when nothing usable is found.</summary>
        public static LedesDocument Parse(string text)
        {
            if (text == null)
            {
                throw new ArgumentNullException("text");
            }

            text = text.TrimStart('﻿');
            if (string.IsNullOrWhiteSpace(text))
            {
                throw new LedesFormatException("The file is empty.");
            }

            if (text.TrimStart().StartsWith("<", StringComparison.Ordinal))
            {
                return ParseXml(text);
            }

            return ParseDelimited(text);
        }

        // ---- Delimited (1998B / 1998BI family) -------------------------------

        private static LedesDocument ParseDelimited(string text)
        {
            LedesDocument doc = new LedesDocument();
            List<string> records = SplitRecords(text, doc.ParseIssues);

            if (records.Count == 0)
            {
                throw new LedesFormatException("No LEDES records were found in the file.");
            }

            int next = 0;

            // 1. Identifier line, such as "LEDES1998B[]".
            LedesFormat format = null;
            if (records[0].IndexOf('|') < 0)
            {
                doc.Identifier = records[0].Trim();
                format = LedesFormat.FromIdentifier(doc.Identifier);
                if (format == null)
                {
                    doc.ParseIssues.Add(new LedesIssue(IssueSeverity.Warning, -1, null,
                        "The first line \"" + doc.Identifier + "\" isn't a recognised LEDES identifier. " +
                        "It will be written back unchanged."));
                }

                next = 1;
            }
            else
            {
                doc.ParseIssues.Add(new LedesIssue(IssueSeverity.Warning, -1, null,
                    "The file has no identifier line (such as LEDES1998B[]). One will be added when you save."));
            }

            if (next >= records.Count)
            {
                throw new LedesFormatException("The file has an identifier line but no header or line items.");
            }

            // 2. Header line with field names.
            string[] firstFields = records[next].Split('|');
            string[] header;
            if (LooksLikeHeader(firstFields))
            {
                header = firstFields.Select(h => h.Trim()).ToArray();
                next++;
            }
            else
            {
                LedesFormat byCount = format ?? GuessFormatByCount(firstFields.Length);
                if (byCount == null || !byCount.CanConvertTo)
                {
                    throw new LedesFormatException(
                        "The file has no header line, and its " + firstFields.Length +
                        " fields don't match a known LEDES layout (24 for 1998B, 52 for 1998BI V2).");
                }

                header = byCount.Fields.Select(f => f.Name).ToArray();
                doc.ParseIssues.Add(new LedesIssue(IssueSeverity.Warning, -1, null,
                    "The file has no header line. The standard " + byCount.Name + " field names were used."));
            }

            // 3. Settle the format.
            if (format == null)
            {
                format = GuessFormatByHeader(header);
                if (doc.Identifier == null && format.Identifier != null)
                {
                    doc.Identifier = format.Identifier;
                }
            }

            doc.Format = format;
            if (string.IsNullOrEmpty(doc.Identifier))
            {
                doc.Identifier = format.Identifier;
            }

            CheckHeaderAgainstFormat(header, format, doc.ParseIssues);

            // 4. Line items.
            DataTable dt = LedesDocument.CreateTable(header);
            int columns = dt.Columns.Count;

            for (int i = next; i < records.Count; i++)
            {
                string[] values = records[i].Split('|');
                int rowIndex = dt.Rows.Count;

                if (values.Length < columns)
                {
                    doc.ParseIssues.Add(new LedesIssue(IssueSeverity.Error, rowIndex, null,
                        "Has " + values.Length + " fields; " + format.Name + " needs " + columns +
                        ". The missing fields were left blank."));
                    Array.Resize(ref values, columns);
                }
                else if (values.Length > columns)
                {
                    doc.ParseIssues.Add(new LedesIssue(IssueSeverity.Error, rowIndex, dt.Columns[columns - 1].ColumnName,
                        "Has " + values.Length + " fields; " + format.Name + " needs " + columns +
                        ". The extra fields were kept in the last column so no data is lost. A stray \"|\" in a description is the usual cause."));
                    string[] merged = new string[columns];
                    Array.Copy(values, merged, columns - 1);
                    merged[columns - 1] = string.Join("|", values, columns - 1, values.Length - (columns - 1));
                    values = merged;
                }

                DataRow row = dt.NewRow();
                for (int c = 0; c < columns; c++)
                {
                    row[c] = values[c] ?? string.Empty;
                }

                dt.Rows.Add(row);
            }

            dt.AcceptChanges();
            doc.Table = dt;

            if (dt.Rows.Count == 0)
            {
                doc.ParseIssues.Add(new LedesIssue(IssueSeverity.Warning, -1, null, "The file has a header but no line items."));
            }

            return doc;
        }

        /// <summary>Splits on the "[]" record terminator, or on line breaks when the file has none.</summary>
        private static List<string> SplitRecords(string text, List<LedesIssue> issues)
        {
            List<string> records = new List<string>();
            bool hasTerminators = text.IndexOf("[]", StringComparison.Ordinal) >= 0;

            IEnumerable<string> raw = hasTerminators
                ? text.Split(new[] { "[]" }, StringSplitOptions.None)
                : text.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None);

            bool foldedBreaks = false;
            foreach (string r in raw)
            {
                // Each record starts after the previous terminator's line break.
                string rec = r.TrimStart('\r', '\n').TrimEnd('\r', '\n');
                if (rec.Trim().Length == 0)
                {
                    continue;
                }

                if (rec.IndexOf('\n') >= 0 || rec.IndexOf('\r') >= 0)
                {
                    rec = rec.Replace("\r\n", " ").Replace('\n', ' ').Replace('\r', ' ');
                    foldedBreaks = true;
                }

                records.Add(rec);
            }

            if (!hasTerminators)
            {
                issues.Add(new LedesIssue(IssueSeverity.Warning, -1, null,
                    "Lines don't end with \"[]\". They were read line by line, and \"[]\" will be added when you save."));
            }

            if (foldedBreaks)
            {
                issues.Add(new LedesIssue(IssueSeverity.Warning, -1, null,
                    "Some records contained line breaks inside a field. They were replaced with spaces."));
            }

            return records;
        }

        private static bool LooksLikeHeader(string[] fields)
        {
            // A header holds field names: INVOICE_DATE appears in every LEDES delimited format.
            return fields.Any(f => string.Equals(f.Trim(), "INVOICE_DATE", StringComparison.OrdinalIgnoreCase));
        }

        private static LedesFormat GuessFormatByCount(int count)
        {
            if (count == LedesFormat.Ledes1998B.Fields.Count)
            {
                return LedesFormat.Ledes1998B;
            }

            if (count == LedesFormat.Ledes1998BIV2.Fields.Count)
            {
                return LedesFormat.Ledes1998BIV2;
            }

            return null;
        }

        private static LedesFormat GuessFormatByHeader(string[] header)
        {
            foreach (LedesFormat f in new[] { LedesFormat.Ledes1998B, LedesFormat.Ledes1998BIV2 })
            {
                if (f.Fields.Count == header.Length &&
                    f.Fields.Select(x => x.Name).SequenceEqual(header, StringComparer.OrdinalIgnoreCase))
                {
                    return f;
                }
            }

            if (header.Length == LedesFormat.Ledes1998B.Fields.Count)
            {
                return LedesFormat.Ledes1998B;
            }

            if (header.Length == LedesFormat.Ledes1998BIV2.Fields.Count)
            {
                return LedesFormat.Ledes1998BIV2;
            }

            return LedesFormat.DelimitedOther;
        }

        private static void CheckHeaderAgainstFormat(string[] header, LedesFormat format, List<LedesIssue> issues)
        {
            if (!format.CanConvertTo)
            {
                return;
            }

            if (header.Length != format.Fields.Count)
            {
                issues.Add(new LedesIssue(IssueSeverity.Error, -1, null,
                    "The header has " + header.Length + " fields; " + format.Name + " has " + format.Fields.Count + "."));
                return;
            }

            List<string> mismatches = new List<string>();
            for (int i = 0; i < header.Length; i++)
            {
                if (!string.Equals(header[i], format.Fields[i].Name, StringComparison.OrdinalIgnoreCase))
                {
                    mismatches.Add("field " + (i + 1) + " is \"" + header[i] + "\", expected \"" + format.Fields[i].Name + "\"");
                }
            }

            if (mismatches.Count > 0)
            {
                issues.Add(new LedesIssue(IssueSeverity.Warning, -1, null,
                    "The header differs from the " + format.Name + " standard: " + string.Join("; ", mismatches.Take(5)) +
                    (mismatches.Count > 5 ? "; and " + (mismatches.Count - 5) + " more." : ".")));
            }
        }

        // ---- XML (LEDES 2000 and XML 2.x) ------------------------------------

        private static LedesDocument ParseXml(string text)
        {
            XmlDocument xml = new XmlDocument();
            xml.PreserveWhitespace = true;
            xml.XmlResolver = null;

            XmlReaderSettings settings = new XmlReaderSettings();
            settings.DtdProcessing = DtdProcessing.Prohibit; // Blocks external-entity attacks from untrusted files.
            settings.XmlResolver = null;

            try
            {
                using (StringReader sr = new StringReader(text))
                using (XmlReader reader = XmlReader.Create(sr, settings))
                {
                    xml.Load(reader);
                }
            }
            catch (XmlException ex)
            {
                throw new LedesFormatException(
                    "The file looks like XML but can't be read. " + ex.Message, ex);
            }

            if (xml.DocumentElement == null)
            {
                throw new LedesFormatException("The XML file has no root element.");
            }

            LedesDocument doc = new LedesDocument();
            doc.Xml = xml;
            doc.Format = DetectXmlVersion(xml.DocumentElement);

            if (doc.Format == LedesFormat.XmlOther)
            {
                doc.ParseIssues.Add(new LedesIssue(IssueSeverity.Warning, -1, null,
                    "The XML doesn't state a LEDES version. Fee and expense line items were loaded anyway."));
            }

            List<XmlElement> items = xml.GetElementsByTagName("*")
                .Cast<XmlElement>()
                .Where(IsLineItem)
                .ToList();

            // Column order: the order fields first appear across the file.
            List<string> fieldNames = new List<string>();
            HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (XmlElement item in items)
            {
                foreach (XmlElement child in LeafChildren(item))
                {
                    if (seen.Add(child.LocalName))
                    {
                        fieldNames.Add(child.LocalName);
                    }
                }
            }

            List<string> columns = new List<string> { LedesDocument.XmlRowKeyColumn, LedesDocument.XmlLineTypeColumn,
                LedesDocument.XmlInvoiceColumn, LedesDocument.XmlMatterColumn };
            columns.AddRange(fieldNames.Where(n => !columns.Contains(n)));

            DataTable dt = LedesDocument.CreateTable(columns);
            dt.Columns[LedesDocument.XmlRowKeyColumn].DataType = typeof(string);

            foreach (XmlElement item in items)
            {
                DataRow row = dt.NewRow();
                row[LedesDocument.XmlRowKeyColumn] = doc.XmlRows.Count.ToString(System.Globalization.CultureInfo.InvariantCulture);
                row[LedesDocument.XmlLineTypeColumn] = item.LocalName.ToLowerInvariant();
                row[LedesDocument.XmlInvoiceColumn] = AncestorValue(item, "invoice", "inv_id", "invoice_number", "inv_number");
                row[LedesDocument.XmlMatterColumn] = AncestorValue(item, "matter", "lf_matter_id", "client_matter_id", "matter_id");

                foreach (XmlElement child in LeafChildren(item))
                {
                    if (dt.Columns.Contains(child.LocalName) && string.IsNullOrEmpty(Convert.ToString(row[child.LocalName])))
                    {
                        row[child.LocalName] = child.InnerText;
                    }
                }

                doc.XmlRows.Add(item);
                dt.Rows.Add(row);
            }

            foreach (string ro in new[] { LedesDocument.XmlRowKeyColumn, LedesDocument.XmlLineTypeColumn,
                LedesDocument.XmlInvoiceColumn, LedesDocument.XmlMatterColumn })
            {
                dt.Columns[ro].ReadOnly = true;
            }

            dt.AcceptChanges();
            doc.Table = dt;

            if (items.Count == 0)
            {
                doc.ParseIssues.Add(new LedesIssue(IssueSeverity.Warning, -1, null,
                    "No <fee> or <expense> line items were found. The file can still be viewed in the output tab."));
            }

            return doc;
        }

        private static LedesFormat DetectXmlVersion(XmlElement root)
        {
            StringBuilder hints = new StringBuilder();
            hints.Append(root.LocalName).Append(' ').Append(root.NamespaceURI).Append(' ');
            foreach (XmlAttribute a in root.Attributes)
            {
                hints.Append(a.Name).Append('=').Append(a.Value).Append(' ');
            }

            // Some files carry the version in a child element instead of an attribute.
            foreach (XmlElement child in root.ChildNodes.OfType<XmlElement>().Take(5))
            {
                if (child.LocalName.IndexOf("version", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    hints.Append(child.InnerText).Append(' ');
                }
            }

            string h = hints.ToString().ToLowerInvariant();

            if (h.Contains("2.2") || h.Contains("2_2") || h.Contains("v22"))
            {
                return LedesFormat.Xml22;
            }

            if (h.Contains("2.1") || h.Contains("2_1") || h.Contains("v21"))
            {
                return LedesFormat.Xml21;
            }

            if (h.Contains("2000"))
            {
                return LedesFormat.Xml2000;
            }

            if (h.Contains("2.0") || h.Contains("2_0") || h.Contains("v20"))
            {
                return LedesFormat.Xml20;
            }

            return LedesFormat.XmlOther;
        }

        private static bool IsLineItem(XmlElement e)
        {
            if (!IsLineItemName(e.LocalName))
            {
                return false;
            }

            // Only leaf-bearing elements count, and never one nested inside another line item.
            if (!LeafChildren(e).Any())
            {
                return false;
            }

            XmlElement parent = e.ParentNode as XmlElement;
            return parent == null || !IsLineItemName(parent.LocalName);
        }

        private static bool IsLineItemName(string name)
        {
            return string.Equals(name, "fee", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(name, "expense", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Child elements that hold a value rather than further elements.</summary>
        internal static IEnumerable<XmlElement> LeafChildren(XmlElement e)
        {
            return e.ChildNodes.OfType<XmlElement>().Where(c => !c.ChildNodes.OfType<XmlElement>().Any());
        }

        private static string AncestorValue(XmlElement item, string ancestorName, params string[] fieldNames)
        {
            for (XmlNode n = item.ParentNode; n != null; n = n.ParentNode)
            {
                XmlElement e = n as XmlElement;
                if (e == null || !string.Equals(e.LocalName, ancestorName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                foreach (string f in fieldNames)
                {
                    XmlElement hit = LeafChildren(e).FirstOrDefault(c => string.Equals(c.LocalName, f, StringComparison.OrdinalIgnoreCase));
                    if (hit != null)
                    {
                        return hit.InnerText;
                    }
                }

                return string.Empty;
            }

            return string.Empty;
        }
    }
}
