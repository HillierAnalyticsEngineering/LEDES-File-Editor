using System;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

namespace Hillier_LEDES_Reader.Ledes
{
    /// <summary>Turns a <see cref="LedesDocument"/> back into LEDES text.</summary>
    public static class LedesWriter
    {
        private const string Terminator = "[]";
        private const string NewLine = "\r\n";

        /// <summary>Renders the document. XML edits are copied into the XML tree first.</summary>
        public static string Serialize(LedesDocument doc)
        {
            if (doc == null)
            {
                throw new ArgumentNullException("doc");
            }

            if (doc.Table == null)
            {
                throw new InvalidOperationException("There is no data to write.");
            }

            return doc.IsXml ? SerializeXml(doc) : SerializeDelimited(doc);
        }

        /// <summary>Writes to disk through a temporary file, so a failed save never leaves a half-written invoice.</summary>
        public static void Save(LedesDocument doc, string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("No file name was given.", "path");
            }

            string text = Serialize(doc);
            Encoding encoding = new UTF8Encoding(false);
            string dir = Path.GetDirectoryName(Path.GetFullPath(path));
            string temp = Path.Combine(dir, "." + Path.GetFileName(path) + "." + Guid.NewGuid().ToString("N") + ".tmp");

            try
            {
                File.WriteAllText(temp, text, encoding);
                if (File.Exists(path))
                {
                    File.Replace(temp, path, null);
                }
                else
                {
                    File.Move(temp, path);
                }
            }
            finally
            {
                try
                {
                    if (File.Exists(temp))
                    {
                        File.Delete(temp);
                    }
                }
                catch (IOException)
                {
                    // The temp file is hidden and harmless. Don't mask the real error.
                }
                catch (UnauthorizedAccessException)
                {
                }
            }

            doc.FilePath = path;
        }

        private static string SerializeDelimited(LedesDocument doc)
        {
            StringBuilder sb = new StringBuilder();
            string identifier = string.IsNullOrWhiteSpace(doc.Identifier)
                ? (doc.Format != null && doc.Format.Identifier != null ? doc.Format.Identifier : LedesFormat.Ledes1998B.Identifier)
                : doc.Identifier.Trim();

            if (identifier.EndsWith(Terminator, StringComparison.Ordinal))
            {
                identifier = identifier.Substring(0, identifier.Length - Terminator.Length);
            }

            sb.Append(identifier).Append(Terminator).Append(NewLine);

            DataColumn[] cols = doc.DataColumns.ToArray();
            sb.Append(string.Join("|", cols.Select(c => c.ColumnName))).Append(Terminator).Append(NewLine);

            foreach (DataRow row in doc.Table.Rows)
            {
                if (row.RowState == DataRowState.Deleted || IsEmptyRow(row, cols))
                {
                    continue;
                }

                sb.Append(string.Join("|", cols.Select(c => Clean(row[c])))).Append(Terminator).Append(NewLine);
            }

            return sb.ToString();
        }

        private static string SerializeXml(LedesDocument doc)
        {
            if (doc.Xml == null)
            {
                throw new InvalidOperationException("The XML source for this file is missing.");
            }

            ApplyXmlEdits(doc);

            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Encoding = new UTF8Encoding(false);
            settings.Indent = false; // The document keeps its own whitespace.
            settings.NewLineHandling = NewLineHandling.None;

            using (StringWriterUtf8 sw = new StringWriterUtf8())
            {
                using (XmlWriter w = XmlWriter.Create(sw, settings))
                {
                    doc.Xml.Save(w);
                }

                return sw.ToString();
            }
        }

        /// <summary>Copies grid values into the matching XML elements.</summary>
        public static void ApplyXmlEdits(LedesDocument doc)
        {
            DataColumn[] editable = doc.DataColumns.Where(c => !c.ReadOnly).ToArray();

            foreach (DataRow row in doc.Table.Rows)
            {
                if (row.RowState == DataRowState.Deleted)
                {
                    continue;
                }

                int key;
                if (!int.TryParse(Convert.ToString(row[LedesDocument.XmlRowKeyColumn]), NumberStyles.Integer, CultureInfo.InvariantCulture, out key) ||
                    key < 0 || key >= doc.XmlRows.Count)
                {
                    continue; // Rows without a source element can't be placed in the XML tree.
                }

                XmlElement item = doc.XmlRows[key];
                foreach (DataColumn col in editable)
                {
                    string value = Convert.ToString(row[col]);
                    XmlElement field = LedesParser.LeafChildren(item).FirstOrDefault(e => e.LocalName == col.ColumnName);

                    if (field != null)
                    {
                        if (field.InnerText != value)
                        {
                            field.InnerText = value;
                        }
                    }
                    else if (!string.IsNullOrEmpty(value))
                    {
                        XmlElement added = item.OwnerDocument.CreateElement(item.Prefix, col.ColumnName, item.NamespaceURI);
                        added.InnerText = value;
                        item.AppendChild(added);
                    }
                }
            }
        }

        private static string Clean(object value)
        {
            string s = Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;

            // Line breaks would split the record. Pipes and "[]" are reported by validation, not silently changed.
            return s.Replace("\r\n", " ").Replace('\r', ' ').Replace('\n', ' ');
        }

        private static bool IsEmptyRow(DataRow row, DataColumn[] cols)
        {
            return cols.All(c => string.IsNullOrWhiteSpace(Convert.ToString(row[c])));
        }

        private sealed class StringWriterUtf8 : StringWriter
        {
            public StringWriterUtf8() : base(CultureInfo.InvariantCulture) { }

            public override Encoding Encoding
            {
                get { return new UTF8Encoding(false); }
            }
        }
    }
}
