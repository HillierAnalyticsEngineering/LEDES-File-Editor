using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Xml;

namespace Hillier_LEDES_Reader.Ledes
{
    public enum IssueSeverity
    {
        Info,
        Warning,
        Error
    }

    /// <summary>A problem found while reading or checking a LEDES file.</summary>
    public sealed class LedesIssue
    {
        public LedesIssue(IssueSeverity severity, int rowIndex, string column, string message)
        {
            Severity = severity;
            RowIndex = rowIndex;
            Column = column;
            Message = message;
        }

        public IssueSeverity Severity { get; private set; }

        /// <summary>Zero-based index into <see cref="LedesDocument.Table"/>, or -1 for file-level issues.</summary>
        public int RowIndex { get; private set; }

        /// <summary>Column name, or null when the issue is not tied to one field.</summary>
        public string Column { get; private set; }

        public string Message { get; private set; }

        public override string ToString()
        {
            string where = RowIndex >= 0 ? "Row " + (RowIndex + 1) : "File";
            if (!string.IsNullOrEmpty(Column))
            {
                where += ", " + Column;
            }

            return Severity + " - " + where + ": " + Message;
        }
    }

    /// <summary>Thrown when a file cannot be read as LEDES at all.</summary>
    [Serializable]
    public class LedesFormatException : Exception
    {
        public LedesFormatException(string message) : base(message) { }
        public LedesFormatException(string message, Exception inner) : base(message, inner) { }
        protected LedesFormatException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }
    }

    /// <summary>
    /// An open LEDES file. Line items are exposed as a <see cref="DataTable"/> for the grid,
    /// whatever the underlying format.
    /// </summary>
    public sealed class LedesDocument
    {
        /// <summary>Name of the hidden column that links an XML row back to its element.</summary>
        public const string XmlRowKeyColumn = "__XML_ROW__";

        /// <summary>Read-only column showing whether an XML line item is a fee or an expense.</summary>
        public const string XmlLineTypeColumn = "LINE_TYPE";

        public const string XmlInvoiceColumn = "INVOICE";
        public const string XmlMatterColumn = "MATTER";

        public LedesDocument()
        {
            ParseIssues = new List<LedesIssue>();
            XmlRows = new List<XmlElement>();
        }

        public LedesFormat Format { get; set; }

        public DataTable Table { get; set; }

        /// <summary>Identifier line as it will be written, without "[]". Delimited files only.</summary>
        public string Identifier { get; set; }

        /// <summary>Source XML. Edits in <see cref="Table"/> are written back into it on save.</summary>
        public XmlDocument Xml { get; set; }

        /// <summary>Element behind each XML row, indexed by the value of <see cref="XmlRowKeyColumn"/>.</summary>
        public List<XmlElement> XmlRows { get; private set; }

        /// <summary>Problems found while reading. Validation results are kept separately.</summary>
        public List<LedesIssue> ParseIssues { get; private set; }

        /// <summary>Path of the file on disk, or null for the bundled example or a new file.</summary>
        public string FilePath { get; set; }

        public bool IsXml
        {
            get { return Format != null && Format.Encoding == LedesEncoding.Xml; }
        }

        /// <summary>Columns a person can see and edit, in display order.</summary>
        public IEnumerable<DataColumn> DataColumns
        {
            get { return Table.Columns.Cast<DataColumn>().Where(c => c.ColumnName != XmlRowKeyColumn); }
        }

        /// <summary>Builds an empty delimited table for a known format.</summary>
        public static DataTable CreateTable(IEnumerable<string> columnNames)
        {
            DataTable dt = new DataTable("LEDES");
            HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (string raw in columnNames)
            {
                string name = string.IsNullOrWhiteSpace(raw) ? "FIELD_" + (dt.Columns.Count + 1) : raw.Trim();

                // DataTable needs unique names; duplicate header fields get a numbered suffix.
                string unique = name;
                int n = 2;
                while (!seen.Add(unique))
                {
                    unique = name + "_" + n++;
                }

                DataColumn col = new DataColumn(unique, typeof(string));
                col.DefaultValue = string.Empty;
                dt.Columns.Add(col);
            }

            return dt;
        }

        /// <summary>
        /// Re-shapes a delimited document into another delimited format. Columns are matched by name.
        /// Fields the target needs but the source lacks are left blank, except tax totals, which become 0.
        /// </summary>
        public LedesDocument ConvertTo(LedesFormat target)
        {
            if (target == null)
            {
                throw new ArgumentNullException("target");
            }

            if (IsXml)
            {
                throw new InvalidOperationException("XML files can't be converted to a delimited format.");
            }

            if (!target.CanConvertTo)
            {
                throw new InvalidOperationException(target.Name + " has no fixed field list to convert into.");
            }

            DataTable dt = CreateTable(target.Fields.Select(f => f.Name));
            List<string> dropped = DataColumns
                .Select(c => c.ColumnName)
                .Where(n => target.FindField(n) == null && !IsBlankColumn(n))
                .ToList();

            foreach (DataRow src in Table.Rows)
            {
                if (src.RowState == DataRowState.Deleted)
                {
                    continue;
                }

                DataRow row = dt.NewRow();
                foreach (LedesField f in target.Fields)
                {
                    string value = Table.Columns.Contains(f.Name) ? Convert.ToString(src[f.Name]) : null;
                    if (string.IsNullOrEmpty(value) && f.Name == "LINE_ITEM_TAX_TOTAL")
                    {
                        value = "0";
                    }

                    if (string.IsNullOrEmpty(value) && f.Name == "INVOICE_TAX_TOTAL")
                    {
                        value = "0";
                    }

                    if (string.IsNullOrEmpty(value) && f.Name == "INVOICE_NET_TOTAL" && Table.Columns.Contains("INVOICE_TOTAL"))
                    {
                        // With no tax recorded, the net total equals the invoice total.
                        value = Convert.ToString(src["INVOICE_TOTAL"]);
                    }

                    row[f.Name] = value ?? string.Empty;
                }

                dt.Rows.Add(row);
            }

            LedesDocument doc = new LedesDocument();
            doc.Format = target;
            doc.Identifier = target.Identifier;
            doc.Table = dt;
            doc.FilePath = null;

            if (dropped.Count > 0)
            {
                doc.ParseIssues.Add(new LedesIssue(IssueSeverity.Warning, -1, null,
                    target.Name + " has no field for: " + string.Join(", ", dropped) + ". Those values were dropped."));
            }

            return doc;
        }

        private bool IsBlankColumn(string name)
        {
            foreach (DataRow r in Table.Rows)
            {
                if (r.RowState != DataRowState.Deleted && !string.IsNullOrWhiteSpace(Convert.ToString(r[name])))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
