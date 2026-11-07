using System;
using System.Collections.Generic;
using System.Linq;

namespace Hillier_LEDES_Reader.Ledes
{
    /// <summary>How a LEDES file stores its records.</summary>
    public enum LedesEncoding
    {
        /// <summary>Pipe-delimited ASCII records ending in "[]" (LEDES 1998B and the 1998BI family).</summary>
        Delimited,
        /// <summary>XML documents (LEDES 2000 and LEDES XML 2.x).</summary>
        Xml
    }

    /// <summary>The kind of value a field holds. Drives validation.</summary>
    public enum LedesFieldType
    {
        Text,
        /// <summary>CCYYMMDD in the delimited formats.</summary>
        Date,
        /// <summary>Money amount, may be negative.</summary>
        Currency,
        /// <summary>Plain decimal number such as units or a tax rate.</summary>
        Decimal,
        /// <summary>F, E, IF or IE.</summary>
        LineType
    }

    /// <summary>One column of a delimited LEDES format.</summary>
    public sealed class LedesField
    {
        public LedesField(string name, LedesFieldType type, int maxLength, bool required)
        {
            Name = name;
            Type = type;
            MaxLength = maxLength;
            Required = required;
        }

        public string Name { get; private set; }
        public LedesFieldType Type { get; private set; }

        /// <summary>Maximum characters, or 0 when the format sets no limit we enforce.</summary>
        public int MaxLength { get; private set; }

        public bool Required { get; private set; }
    }

    /// <summary>A known LEDES format with its identifying first line and its field list.</summary>
    public sealed class LedesFormat
    {
        private LedesFormat(string name, string identifier, LedesEncoding encoding, IList<LedesField> fields, string description)
        {
            Name = name;
            Identifier = identifier;
            Encoding = encoding;
            Fields = fields == null ? new List<LedesField>() : new List<LedesField>(fields);
            Description = description;
        }

        /// <summary>Display name, for example "LEDES 1998B".</summary>
        public string Name { get; private set; }

        /// <summary>Text of the first line without the trailing "[]" (delimited formats only).</summary>
        public string Identifier { get; private set; }

        public LedesEncoding Encoding { get; private set; }

        /// <summary>Ordered field list. Empty for XML formats, whose columns come from the document.</summary>
        public IReadOnlyList<LedesField> Fields { get; private set; }

        public string Description { get; private set; }

        /// <summary>True when the editor can convert other delimited files into this format.</summary>
        public bool CanConvertTo
        {
            get { return Encoding == LedesEncoding.Delimited && Fields.Count > 0; }
        }

        public override string ToString()
        {
            return Name;
        }

        public LedesField FindField(string name)
        {
            return Fields.FirstOrDefault(f => string.Equals(f.Name, name, StringComparison.OrdinalIgnoreCase));
        }

        // ---- Field catalogue -------------------------------------------------

        private static readonly LedesField[] Fields1998B =
        {
            new LedesField("INVOICE_DATE", LedesFieldType.Date, 8, true),
            new LedesField("INVOICE_NUMBER", LedesFieldType.Text, 0, true),
            new LedesField("CLIENT_ID", LedesFieldType.Text, 0, true),
            new LedesField("LAW_FIRM_MATTER_ID", LedesFieldType.Text, 0, true),
            new LedesField("INVOICE_TOTAL", LedesFieldType.Currency, 0, true),
            new LedesField("BILLING_START_DATE", LedesFieldType.Date, 8, true),
            new LedesField("BILLING_END_DATE", LedesFieldType.Date, 8, true),
            new LedesField("INVOICE_DESCRIPTION", LedesFieldType.Text, 0, false),
            new LedesField("LINE_ITEM_NUMBER", LedesFieldType.Text, 0, true),
            new LedesField("EXP/FEE/INV_ADJ_TYPE", LedesFieldType.LineType, 2, true),
            new LedesField("LINE_ITEM_NUMBER_OF_UNITS", LedesFieldType.Decimal, 0, true),
            new LedesField("LINE_ITEM_ADJUSTMENT_AMOUNT", LedesFieldType.Currency, 0, false),
            new LedesField("LINE_ITEM_TOTAL", LedesFieldType.Currency, 0, true),
            new LedesField("LINE_ITEM_DATE", LedesFieldType.Date, 8, true),
            new LedesField("LINE_ITEM_TASK_CODE", LedesFieldType.Text, 0, false),
            new LedesField("LINE_ITEM_EXPENSE_CODE", LedesFieldType.Text, 0, false),
            new LedesField("LINE_ITEM_ACTIVITY_CODE", LedesFieldType.Text, 0, false),
            new LedesField("TIMEKEEPER_ID", LedesFieldType.Text, 0, false),
            new LedesField("LINE_ITEM_DESCRIPTION", LedesFieldType.Text, 0, false),
            new LedesField("LAW_FIRM_ID", LedesFieldType.Text, 0, true),
            new LedesField("LINE_ITEM_UNIT_COST", LedesFieldType.Currency, 0, true),
            new LedesField("TIMEKEEPER_NAME", LedesFieldType.Text, 0, false),
            new LedesField("TIMEKEEPER_CLASSIFICATION", LedesFieldType.Text, 0, false),
            new LedesField("CLIENT_MATTER_ID", LedesFieldType.Text, 0, false)
        };

        // Fields 25-52 of LEDES98BI V2, in file order.
        private static readonly LedesField[] FieldsBIV2Extra =
        {
            new LedesField("PO_NUMBER", LedesFieldType.Text, 100, false),
            new LedesField("CLIENT_TAX_ID", LedesFieldType.Text, 20, false),
            new LedesField("MATTER_NAME", LedesFieldType.Text, 255, false),
            new LedesField("INVOICE_TAX_TOTAL", LedesFieldType.Currency, 0, false),
            new LedesField("INVOICE_NET_TOTAL", LedesFieldType.Currency, 0, true),
            new LedesField("INVOICE_CURRENCY", LedesFieldType.Text, 3, false),
            new LedesField("TIMEKEEPER_LAST_NAME", LedesFieldType.Text, 30, false),
            new LedesField("TIMEKEEPER_FIRST_NAME", LedesFieldType.Text, 30, false),
            new LedesField("ACCOUNT_TYPE", LedesFieldType.Text, 1, false),
            new LedesField("LAW_FIRM_NAME", LedesFieldType.Text, 60, false),
            new LedesField("LAW_FIRM_ADDRESS_1", LedesFieldType.Text, 60, false),
            new LedesField("LAW_FIRM_ADDRESS_2", LedesFieldType.Text, 60, false),
            new LedesField("LAW_FIRM_CITY", LedesFieldType.Text, 40, false),
            new LedesField("LAW_FIRM_STATEorREGION", LedesFieldType.Text, 40, false),
            new LedesField("LAW_FIRM_POSTCODE", LedesFieldType.Text, 20, false),
            new LedesField("LAW_FIRM_COUNTRY", LedesFieldType.Text, 3, false),
            new LedesField("CLIENT_NAME", LedesFieldType.Text, 60, false),
            new LedesField("CLIENT_ADDRESS_1", LedesFieldType.Text, 60, false),
            new LedesField("CLIENT_ADDRESS_2", LedesFieldType.Text, 60, false),
            new LedesField("CLIENT_CITY", LedesFieldType.Text, 40, false),
            new LedesField("CLIENT_STATEorREGION", LedesFieldType.Text, 40, false),
            new LedesField("CLIENT_POSTCODE", LedesFieldType.Text, 20, false),
            new LedesField("CLIENT_COUNTRY", LedesFieldType.Text, 3, false),
            new LedesField("LINE_ITEM_TAX_RATE", LedesFieldType.Decimal, 0, false),
            new LedesField("LINE_ITEM_TAX_TOTAL", LedesFieldType.Currency, 0, true),
            new LedesField("LINE_ITEM_TAX_TYPE", LedesFieldType.Text, 20, false),
            new LedesField("INVOICE_REPORTED_TAX_TOTAL", LedesFieldType.Currency, 0, false),
            new LedesField("INVOICE_TAX_CURRENCY", LedesFieldType.Text, 3, false)
        };

        public static readonly LedesFormat Ledes1998B = new LedesFormat(
            "LEDES 1998B", "LEDES1998B", LedesEncoding.Delimited, Fields1998B,
            "Pipe-delimited, 24 fields. The most widely used US e-billing format.");

        public static readonly LedesFormat Ledes1998BIV2 = new LedesFormat(
            "LEDES 1998BI V2", "LEDES98BI V2", LedesEncoding.Delimited, Fields1998B.Concat(FieldsBIV2Extra).ToList(),
            "Pipe-delimited, 52 fields. The international format with tax, currency and address fields.");

        /// <summary>
        /// The first international release. Its column list is taken from the file's own header line,
        /// and validation uses the matching 1998B and BI V2 field rules by name.
        /// </summary>
        public static readonly LedesFormat Ledes1998BI = new LedesFormat(
            "LEDES 1998BI", "LEDES98BI", LedesEncoding.Delimited, null,
            "Pipe-delimited international format (first release). Columns are read from the file header.");

        /// <summary>A delimited file whose first line is not a known identifier. Columns come from the header.</summary>
        public static readonly LedesFormat DelimitedOther = new LedesFormat(
            "Other delimited LEDES", null, LedesEncoding.Delimited, null,
            "Pipe-delimited file with an unrecognised identifier line. Columns are read from the file header.");

        public static readonly LedesFormat Xml2000 = new LedesFormat(
            "LEDES 2000 (XML)", null, LedesEncoding.Xml, null, "LEDES 2000 XML invoice.");

        public static readonly LedesFormat Xml20 = new LedesFormat(
            "LEDES XML 2.0", null, LedesEncoding.Xml, null, "LEDES XML e-billing version 2.0.");

        public static readonly LedesFormat Xml21 = new LedesFormat(
            "LEDES XML 2.1", null, LedesEncoding.Xml, null, "LEDES XML e-billing version 2.1.");

        public static readonly LedesFormat Xml22 = new LedesFormat(
            "LEDES XML 2.2", null, LedesEncoding.Xml, null, "LEDES XML e-billing version 2.2.");

        public static readonly LedesFormat XmlOther = new LedesFormat(
            "LEDES XML (version not stated)", null, LedesEncoding.Xml, null, "XML e-billing file with no recognised version.");

        /// <summary>Every format the editor understands, in menu order.</summary>
        public static IReadOnlyList<LedesFormat> All
        {
            get
            {
                return new[]
                {
                    Ledes1998B, Ledes1998BIV2, Ledes1998BI, DelimitedOther,
                    Xml2000, Xml20, Xml21, Xml22, XmlOther
                };
            }
        }

        /// <summary>Formats a delimited document can be converted into.</summary>
        public static IReadOnlyList<LedesFormat> ConvertTargets
        {
            get { return All.Where(f => f.CanConvertTo).ToList(); }
        }

        /// <summary>Looks up a delimited format by its first-line identifier (with or without "[]").</summary>
        public static LedesFormat FromIdentifier(string firstLine)
        {
            if (firstLine == null)
            {
                return null;
            }

            string id = firstLine.Trim();
            if (id.EndsWith("[]", StringComparison.Ordinal))
            {
                id = id.Substring(0, id.Length - 2).Trim();
            }

            // Collapse runs of spaces so "LEDES98BI  V2" still matches.
            id = string.Join(" ", id.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries));

            foreach (LedesFormat f in new[] { Ledes1998B, Ledes1998BIV2, Ledes1998BI })
            {
                if (string.Equals(f.Identifier, id, StringComparison.OrdinalIgnoreCase))
                {
                    return f;
                }
            }

            // Common hand-typed variants.
            if (string.Equals(id, "LEDES98B", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(id, "LEDES 1998B", StringComparison.OrdinalIgnoreCase))
            {
                return Ledes1998B;
            }

            if (string.Equals(id, "LEDES1998BI V2", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(id, "LEDES98BIV2", StringComparison.OrdinalIgnoreCase))
            {
                return Ledes1998BIV2;
            }

            return null;
        }

        /// <summary>Finds the rule for a column name across every known delimited format.</summary>
        public static LedesField FindFieldRule(string columnName)
        {
            return Ledes1998BIV2.FindField(columnName);
        }
    }
}
