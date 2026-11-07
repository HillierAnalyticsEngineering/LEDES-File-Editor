using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;

namespace Hillier_LEDES_Reader.Ledes
{
    /// <summary>
    /// Checks a document against the LEDES rules most e-billing systems reject on.
    /// Rules are matched by column name, so they apply to every delimited variant.
    /// </summary>
    public static class LedesValidator
    {
        /// <summary>Totals may differ by up to a cent from rounding.</summary>
        private const decimal Tolerance = 0.01m;

        private static readonly string[] LineTypes = { "F", "E", "IF", "IE" };

        private static readonly string[] XmlNumericFields =
        {
            "units", "rate", "base_amount", "total_amount", "item_disc_cred_amount", "disc_cred_amount", "discount_percent"
        };

        private static readonly string[] XmlDateFields = { "charge_date", "inv_date", "inv_start_date", "inv_end_date" };

        public static List<LedesIssue> Validate(LedesDocument doc)
        {
            List<LedesIssue> issues = new List<LedesIssue>();
            if (doc == null || doc.Table == null)
            {
                return issues;
            }

            List<DataRow> rows = doc.Table.Rows.Cast<DataRow>().ToList();
            if (rows.All(r => r.RowState == DataRowState.Deleted))
            {
                issues.Add(new LedesIssue(IssueSeverity.Warning, -1, null, "The file has no line items."));
                return issues;
            }

            if (doc.IsXml)
            {
                ValidateXml(doc, rows, issues);
            }
            else
            {
                ValidateDelimited(doc, rows, issues);
            }

            return issues;
        }

        // ---- Delimited -----------------------------------------------------------

        private static void ValidateDelimited(LedesDocument doc, List<DataRow> rows, List<LedesIssue> issues)
        {
            DataColumn[] cols = doc.DataColumns.ToArray();
            bool hasTax = doc.Table.Columns.Contains("LINE_ITEM_TAX_TOTAL");

            for (int i = 0; i < rows.Count; i++)
            {
                DataRow row = rows[i];
                if (row.RowState == DataRowState.Deleted || cols.All(c => string.IsNullOrWhiteSpace(Get(row, c.ColumnName))))
                {
                    continue;
                }

                foreach (DataColumn col in cols)
                {
                    CheckField(i, col.ColumnName, Get(row, col.ColumnName), issues);
                }

                CheckLineItem(doc, row, i, hasTax, issues);
            }

            CheckInvoices(doc, rows, hasTax, issues);
        }

        private static void CheckField(int rowIndex, string column, string value, List<LedesIssue> issues)
        {
            if (value.IndexOf('|') >= 0)
            {
                issues.Add(Err(rowIndex, column, "Contains \"|\", which is the LEDES field separator. Remove it or the file will be misread."));
            }

            if (value.Contains("[]"))
            {
                issues.Add(Err(rowIndex, column, "Contains \"[]\", which marks the end of a LEDES record. Remove it."));
            }

            if (value.Any(ch => ch > 126 || (ch < 32 && ch != '\t')))
            {
                issues.Add(Warn(rowIndex, column, "Contains characters outside plain ASCII. Some e-billing systems reject them."));
            }

            LedesField rule = LedesFormat.FindFieldRule(column);
            if (rule == null)
            {
                return;
            }

            string v = value.Trim();
            if (v.Length == 0)
            {
                if (rule.Required)
                {
                    issues.Add(Err(rowIndex, column, "Required field is blank."));
                }

                return;
            }

            switch (rule.Type)
            {
                case LedesFieldType.Date:
                    DateTime d;
                    if (!TryParseLedesDate(v, out d))
                    {
                        issues.Add(Err(rowIndex, column, "\"" + v + "\" isn't a valid date. Use CCYYMMDD, for example 20260131."));
                    }

                    break;

                case LedesFieldType.Currency:
                case LedesFieldType.Decimal:
                    decimal n;
                    if (!TryParseNumber(v, out n))
                    {
                        issues.Add(Err(rowIndex, column, "\"" + v + "\" isn't a number. Use digits and a decimal point only, with no currency symbol or thousands separator."));
                    }
                    else if (rule.Type == LedesFieldType.Currency && DecimalPlaces(v) > 4)
                    {
                        issues.Add(Warn(rowIndex, column, "Has more than 4 decimal places."));
                    }

                    break;

                case LedesFieldType.LineType:
                    if (!LineTypes.Contains(v, StringComparer.Ordinal))
                    {
                        issues.Add(Err(rowIndex, column, "\"" + v + "\" isn't a line type. Use F (fee), E (expense), IF (invoice fee adjustment) or IE (invoice expense adjustment)."));
                    }

                    break;
            }

            // Dates and line types already fail their format check when too long.
            if (rule.MaxLength > 0 && v.Length > rule.MaxLength && rule.Type != LedesFieldType.Date && rule.Type != LedesFieldType.LineType)
            {
                issues.Add(Warn(rowIndex, column, "Is " + v.Length + " characters; the limit is " + rule.MaxLength + "."));
            }
        }

        private static void CheckLineItem(LedesDocument doc, DataRow row, int i, bool hasTax, List<LedesIssue> issues)
        {
            string type = Get(row, "EXP/FEE/INV_ADJ_TYPE").Trim();

            if (type == "F" && doc.Table.Columns.Contains("TIMEKEEPER_ID") && Get(row, "TIMEKEEPER_ID").Trim().Length == 0)
            {
                issues.Add(Warn(i, "TIMEKEEPER_ID", "Fee lines usually need a timekeeper ID."));
            }

            if (type == "E" && doc.Table.Columns.Contains("LINE_ITEM_EXPENSE_CODE") && Get(row, "LINE_ITEM_EXPENSE_CODE").Trim().Length == 0)
            {
                issues.Add(Warn(i, "LINE_ITEM_EXPENSE_CODE", "Expense lines usually need a UTBMS expense code."));
            }

            // Line total arithmetic. Invoice-level adjustments (IF/IE) follow client-specific conventions, so skip them.
            if (type == "F" || type == "E")
            {
                decimal units, cost, total;
                if (TryNumber(row, "LINE_ITEM_NUMBER_OF_UNITS", out units) &&
                    TryNumber(row, "LINE_ITEM_UNIT_COST", out cost) &&
                    TryNumber(row, "LINE_ITEM_TOTAL", out total))
                {
                    decimal adj = NumberOrZero(row, "LINE_ITEM_ADJUSTMENT_AMOUNT");
                    decimal tax = hasTax ? NumberOrZero(row, "LINE_ITEM_TAX_TOTAL") : 0m;
                    decimal expected = Math.Round(units * cost + adj + tax, 2, MidpointRounding.AwayFromZero);

                    if (Math.Abs(expected - total) > Tolerance)
                    {
                        string formula = hasTax ? "units x unit cost + adjustment + tax" : "units x unit cost + adjustment";
                        issues.Add(Err(i, "LINE_ITEM_TOTAL",
                            "Is " + Fmt(total) + " but " + formula + " = " + Fmt(expected) + "."));
                    }
                }
            }

            DateTime start, end, line;
            bool hasStart = TryParseLedesDate(Get(row, "BILLING_START_DATE").Trim(), out start);
            bool hasEnd = TryParseLedesDate(Get(row, "BILLING_END_DATE").Trim(), out end);

            if (hasStart && hasEnd && start > end)
            {
                issues.Add(Err(i, "BILLING_START_DATE", "Billing start date is after the billing end date."));
            }

            if (hasStart && hasEnd && TryParseLedesDate(Get(row, "LINE_ITEM_DATE").Trim(), out line) && (line < start || line > end))
            {
                issues.Add(Warn(i, "LINE_ITEM_DATE", "Falls outside the billing period."));
            }
        }

        private static void CheckInvoices(LedesDocument doc, List<DataRow> rows, bool hasTax, List<LedesIssue> issues)
        {
            if (!doc.Table.Columns.Contains("INVOICE_NUMBER"))
            {
                return;
            }

            var groups = rows
                .Select((r, i) => new { Row = r, Index = i })
                .Where(x => x.Row.RowState != DataRowState.Deleted && Get(x.Row, "INVOICE_NUMBER").Trim().Length > 0)
                .GroupBy(x => Get(x.Row, "INVOICE_NUMBER").Trim(), StringComparer.Ordinal);

            foreach (var g in groups)
            {
                int first = g.First().Index;
                string inv = "Invoice " + g.Key;

                // Invoice-level fields repeat on every line and must agree.
                foreach (string field in new[] { "INVOICE_DATE", "INVOICE_TOTAL", "CLIENT_ID", "LAW_FIRM_MATTER_ID",
                    "BILLING_START_DATE", "BILLING_END_DATE", "INVOICE_TAX_TOTAL", "INVOICE_NET_TOTAL", "INVOICE_CURRENCY" })
                {
                    if (!doc.Table.Columns.Contains(field))
                    {
                        continue;
                    }

                    // Blank values are reported as missing fields, so compare only the filled-in ones.
                    List<string> distinct = g.Select(x => Get(x.Row, field).Trim()).Where(v => v.Length > 0)
                        .Distinct(StringComparer.Ordinal).ToList();
                    if (distinct.Count > 1)
                    {
                        issues.Add(Err(first, field, inv + " has different values on different lines: " + string.Join(", ", distinct.Take(4)) + "."));
                    }
                }

                // Line item numbers should be unique within the invoice.
                if (doc.Table.Columns.Contains("LINE_ITEM_NUMBER"))
                {
                    foreach (var dup in g.GroupBy(x => Get(x.Row, "LINE_ITEM_NUMBER").Trim()).Where(d => d.Key.Length > 0 && d.Count() > 1))
                    {
                        issues.Add(Warn(dup.Skip(1).First().Index, "LINE_ITEM_NUMBER", inv + " uses line item number " + dup.Key + " more than once."));
                    }
                }

                // INVOICE_TOTAL must equal the sum of LINE_ITEM_TOTAL.
                decimal invoiceTotal;
                List<decimal> lineTotals = new List<decimal>();
                bool allNumeric = true;
                foreach (var x in g)
                {
                    decimal t;
                    if (TryNumber(x.Row, "LINE_ITEM_TOTAL", out t))
                    {
                        lineTotals.Add(t);
                    }
                    else
                    {
                        allNumeric = false;
                    }
                }

                if (allNumeric && TryNumber(g.First().Row, "INVOICE_TOTAL", out invoiceTotal))
                {
                    decimal sum = lineTotals.Sum();
                    if (Math.Abs(sum - invoiceTotal) > Tolerance)
                    {
                        issues.Add(Err(first, "INVOICE_TOTAL", inv + " total is " + Fmt(invoiceTotal) + " but its line items add up to " + Fmt(sum) + "."));
                    }
                }

                if (!hasTax)
                {
                    continue;
                }

                decimal taxSum = g.Sum(x => NumberOrZero(x.Row, "LINE_ITEM_TAX_TOTAL"));
                decimal invTax;
                if (TryNumber(g.First().Row, "INVOICE_TAX_TOTAL", out invTax) && Math.Abs(invTax - taxSum) > Tolerance)
                {
                    issues.Add(Warn(first, "INVOICE_TAX_TOTAL", inv + " tax total is " + Fmt(invTax) + " but line item tax adds up to " + Fmt(taxSum) + "."));
                }

                decimal net;
                if (TryNumber(g.First().Row, "INVOICE_NET_TOTAL", out net) && TryNumber(g.First().Row, "INVOICE_TOTAL", out invoiceTotal))
                {
                    decimal expectedNet = invoiceTotal - (TryNumber(g.First().Row, "INVOICE_TAX_TOTAL", out invTax) ? invTax : taxSum);
                    if (Math.Abs(expectedNet - net) > Tolerance)
                    {
                        issues.Add(Warn(first, "INVOICE_NET_TOTAL", inv + " net total is " + Fmt(net) + " but invoice total minus tax = " + Fmt(expectedNet) + "."));
                    }
                }
            }
        }

        // ---- XML -------------------------------------------------------------------

        private static void ValidateXml(LedesDocument doc, List<DataRow> rows, List<LedesIssue> issues)
        {
            for (int i = 0; i < rows.Count; i++)
            {
                DataRow row = rows[i];
                if (row.RowState == DataRowState.Deleted)
                {
                    continue;
                }

                foreach (string f in XmlNumericFields)
                {
                    string v = Get(row, f).Trim();
                    decimal n;
                    if (v.Length > 0 && !TryParseNumber(v, out n))
                    {
                        issues.Add(Err(i, f, "\"" + v + "\" isn't a number."));
                    }
                }

                foreach (string f in XmlDateFields)
                {
                    string v = Get(row, f).Trim();
                    DateTime d;
                    if (v.Length > 0 && !DateTime.TryParse(v, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out d))
                    {
                        issues.Add(Err(i, f, "\"" + v + "\" isn't a valid date. LEDES XML uses CCYY-MM-DD."));
                    }
                }

                decimal units, rate, baseAmount;
                if (TryNumber(row, "units", out units) && TryNumber(row, "rate", out rate) && TryNumber(row, "base_amount", out baseAmount))
                {
                    decimal expected = Math.Round(units * rate, 2, MidpointRounding.AwayFromZero);
                    if (Math.Abs(expected - baseAmount) > Tolerance)
                    {
                        issues.Add(Warn(i, "base_amount", "Is " + Fmt(baseAmount) + " but units x rate = " + Fmt(expected) + "."));
                    }
                }
            }
        }

        // ---- Helpers -----------------------------------------------------------------

        public static bool TryParseLedesDate(string value, out DateTime date)
        {
            return DateTime.TryParseExact(value, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
        }

        public static bool TryParseNumber(string value, out decimal number)
        {
            return decimal.TryParse(value, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture, out number);
        }

        private static int DecimalPlaces(string value)
        {
            int dot = value.IndexOf('.');
            return dot < 0 ? 0 : value.Length - dot - 1;
        }

        private static string Get(DataRow row, string column)
        {
            if (!row.Table.Columns.Contains(column))
            {
                return string.Empty;
            }

            object v = row[column];
            return v == null || v == DBNull.Value ? string.Empty : Convert.ToString(v, CultureInfo.InvariantCulture);
        }

        private static bool TryNumber(DataRow row, string column, out decimal value)
        {
            value = 0m;
            string v = Get(row, column).Trim();
            return v.Length > 0 && TryParseNumber(v, out value);
        }

        private static decimal NumberOrZero(DataRow row, string column)
        {
            decimal v;
            return TryNumber(row, column, out v) ? v : 0m;
        }

        private static string Fmt(decimal d)
        {
            return d.ToString("0.00##", CultureInfo.InvariantCulture);
        }

        private static LedesIssue Err(int row, string col, string msg)
        {
            return new LedesIssue(IssueSeverity.Error, row, col, msg);
        }

        private static LedesIssue Warn(int row, string col, string msg)
        {
            return new LedesIssue(IssueSeverity.Warning, row, col, msg);
        }
    }
}
