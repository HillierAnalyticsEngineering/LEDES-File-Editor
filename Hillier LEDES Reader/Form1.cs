using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using Hillier_LEDES_Reader.Ledes;

namespace Hillier_LEDES_Reader
{
    public partial class LEDESeditor : Form
    {
        private const string Example1998BResource = "Hillier_LEDES_Reader.Samples.Example_1998B.LED";
        private const string Example1998BIV2Resource = "Hillier_LEDES_Reader.Samples.Example_1998BI_V2.LED";

        private readonly string startFile;
        private LedesDocument document;
        private List<LedesIssue> issues = new List<LedesIssue>();
        private bool dirty;
        private bool rowsChanged;
        private bool suppressFormatEvent;
        private bool loading;

        public LEDESeditor() : this(null)
        {
        }

        public LEDESeditor(string fileToOpen)
        {
            startFile = fileToOpen;
            InitializeComponent();
        }

        // ---- Startup and shutdown ------------------------------------------------

        private void LEDESeditor_Load(object sender, EventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(startFile) && OpenFile(startFile))
            {
                return;
            }

            // Every launch starts with a working example so there's something to explore.
            if (!LoadExample(Example1998BResource, "Example LEDES 1998B invoice"))
            {
                ShowDocument(BlankDocument(LedesFormat.Ledes1998B), "New LEDES 1998B file");
            }
        }

        private void LEDESeditor_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!ConfirmDiscardChanges())
            {
                e.Cancel = true;
            }
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            Close();
        }

        // ---- Toolbar actions ------------------------------------------------------------

        private void btnLoad_Click(object sender, EventArgs e)
        {
            if (!ConfirmDiscardChanges())
            {
                return;
            }

            using (OpenFileDialog dlg = new OpenFileDialog())
            {
                dlg.Title = "Open a LEDES file";
                dlg.Filter = "LEDES files (*.LED;*.txt;*.xml)|*.LED;*.txt;*.xml|" +
                             "Delimited LEDES (*.LED;*.txt)|*.LED;*.txt|" +
                             "LEDES XML (*.xml)|*.xml|" +
                             "All files (*.*)|*.*";
                dlg.CheckFileExists = true;
                SetInitialDirectory(dlg);

                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    OpenFile(dlg.FileName);
                }
            }
        }

        private void mnuExample1998B_Click(object sender, EventArgs e)
        {
            if (ConfirmDiscardChanges())
            {
                LoadExample(Example1998BResource, "Example LEDES 1998B invoice");
            }
        }

        private void mnuExample1998BIV2_Click(object sender, EventArgs e)
        {
            if (ConfirmDiscardChanges())
            {
                LoadExample(Example1998BIV2Resource, "Example LEDES 1998BI V2 invoice");
            }
        }

        private void btnValidate_Click(object sender, EventArgs e)
        {
            RunValidation(true);
        }

        private void btnPreview_Click(object sender, EventArgs e)
        {
            if (document == null)
            {
                return;
            }

            try
            {
                CommitGridEdits();
                LEDESoutput.Text = LedesWriter.Serialize(document);
                tc1.SelectedTab = tabPage2;
                SetStatus("Preview shows exactly what Save As will write.");
            }
            catch (Exception ex)
            {
                ErrorReporter.Show(this, "build the preview", ex);
            }
        }

        private void btnWrite_Click(object sender, EventArgs e)
        {
            SaveAs();
        }

        private void btnHelp_Click(object sender, EventArgs e)
        {
            ShowHelp();
        }

        private void cboFormat_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (suppressFormatEvent || document == null)
            {
                return;
            }

            LedesFormat target = cboFormat.SelectedItem as LedesFormat;
            if (target == null || target == document.Format)
            {
                return;
            }

            try
            {
                CommitGridEdits();

                DialogResult answer = MessageBox.Show(this,
                    "Convert this file from " + document.Format.Name + " to " + target.Name + "?" + Environment.NewLine + Environment.NewLine +
                    "Columns are matched by name. New fields start blank, and tax totals start at 0." + Environment.NewLine +
                    "Fields the new format doesn't have are dropped.",
                    "Convert format", MessageBoxButtons.OKCancel, MessageBoxIcon.Question);

                if (answer != DialogResult.OK)
                {
                    SelectFormat(document.Format);
                    return;
                }

                LedesDocument converted = document.ConvertTo(target);
                string label = (DisplayName(document) ?? "File") + " (converted)";
                ShowDocument(converted, label);
                dirty = true;
                UpdateTitle();
                SetStatus("Converted to " + target.Name + ". Review the new fields, then Save As.");
            }
            catch (Exception ex)
            {
                ErrorReporter.Show(this, "convert the file", ex);
                SelectFormat(document.Format);
            }
        }

        // ---- Keyboard and drag-and-drop ----------------------------------------------------

        private void LEDESeditor_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F1)
            {
                ShowHelp();
            }
            else if (e.KeyCode == Keys.F5)
            {
                RunValidation(true);
            }
            else if (e.Control && e.KeyCode == Keys.O)
            {
                btnLoad_Click(sender, EventArgs.Empty);
            }
            else if (e.Control && e.KeyCode == Keys.S)
            {
                SaveAs();
            }
            else if (e.Control && e.KeyCode == Keys.P)
            {
                btnPreview_Click(sender, EventArgs.Empty);
            }
            else
            {
                return;
            }

            e.Handled = true;
            e.SuppressKeyPress = true;
        }

        private void LEDESeditor_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = e.Data != null && e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
        }

        private void LEDESeditor_DragDrop(object sender, DragEventArgs e)
        {
            try
            {
                string[] files = e.Data.GetData(DataFormats.FileDrop) as string[];
                if (files != null && files.Length > 0 && ConfirmDiscardChanges())
                {
                    OpenFile(files[0]);
                }
            }
            catch (Exception ex)
            {
                ErrorReporter.Show(this, "open the dropped file", ex);
            }
        }

        // ---- Grid events -----------------------------------------------------------------------

        private void dataGridView1_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (!loading && e.RowIndex >= 0)
            {
                MarkDirty();
            }
        }

        private void dataGridView1_UserAddedRow(object sender, DataGridViewRowEventArgs e)
        {
            rowsChanged = true;
            MarkDirty();
        }

        private void dataGridView1_RowsRemoved(object sender, DataGridViewRowsRemovedEventArgs e)
        {
            if (!loading)
            {
                rowsChanged = true;
                MarkDirty();
            }
        }

        private void dataGridView1_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            // Keep the grid alive; report the bad value instead of throwing.
            e.ThrowException = false;
            string column = e.ColumnIndex >= 0 && e.ColumnIndex < dataGridView1.Columns.Count
                ? dataGridView1.Columns[e.ColumnIndex].HeaderText
                : "a cell";

            string message = e.Exception != null ? e.Exception.Message : "The value couldn't be stored.";
            SetStatus("Row " + (e.RowIndex + 1) + ", " + column + ": " + message);
            ErrorReporter.Log("grid data error", e.Exception);
        }

        private void lvIssues_ItemActivate(object sender, EventArgs e)
        {
            if (lvIssues.SelectedItems.Count == 0)
            {
                return;
            }

            LedesIssue issue = lvIssues.SelectedItems[0].Tag as LedesIssue;
            if (issue == null || issue.RowIndex < 0)
            {
                return;
            }

            try
            {
                DataGridViewRow row = FindGridRow(issue.RowIndex);
                if (row == null)
                {
                    SetStatus("That row is no longer in the file. Run Validate again.");
                    return;
                }

                int col = 0;
                if (!string.IsNullOrEmpty(issue.Column) && dataGridView1.Columns.Contains(issue.Column) &&
                    dataGridView1.Columns[issue.Column].Visible)
                {
                    col = dataGridView1.Columns[issue.Column].Index;
                }
                else
                {
                    col = dataGridView1.Columns.Cast<DataGridViewColumn>().First(c => c.Visible).Index;
                }

                tc1.SelectedTab = tabPage1;
                dataGridView1.CurrentCell = row.Cells[col];
                dataGridView1.Focus();
            }
            catch (Exception ex)
            {
                ErrorReporter.Show(this, "go to that cell", ex);
            }
        }

        // ---- Core operations -------------------------------------------------------------------

        private bool OpenFile(string path)
        {
            try
            {
                Cursor = Cursors.WaitCursor;
                LedesDocument doc = LedesParser.Load(path);
                ShowDocument(doc, Path.GetFileName(path));
                SetStatus("Opened " + Path.GetFileName(path) + " as " + doc.Format.Name + ".");
                return true;
            }
            catch (Exception ex)
            {
                ErrorReporter.Show(this, "open " + SafeFileName(path), ex);
                return false;
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }

        private bool LoadExample(string resourceName, string label)
        {
            try
            {
                string text;
                using (Stream s = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
                {
                    if (s == null)
                    {
                        throw new InvalidOperationException("The example file is missing from this build.");
                    }

                    using (StreamReader r = new StreamReader(s))
                    {
                        text = r.ReadToEnd();
                    }
                }

                LedesDocument doc = LedesParser.Parse(text);
                ShowDocument(doc, label);
                SetStatus("Loaded the " + doc.Format.Name + " example. Edit it, or click Open to load your own file.");
                return true;
            }
            catch (Exception ex)
            {
                ErrorReporter.Show(this, "load the example", ex);
                return false;
            }
        }

        private void ShowDocument(LedesDocument doc, string label)
        {
            loading = true;
            try
            {
                document = doc;
                displayLabel = label;

                dataGridView1.DataSource = null;
                dataGridView1.Columns.Clear();
                dataGridView1.AutoGenerateColumns = true;
                dataGridView1.DataSource = doc.Table;

                bool xml = doc.IsXml;
                dataGridView1.AllowUserToAddRows = !xml;
                dataGridView1.AllowUserToDeleteRows = !xml;

                foreach (DataGridViewColumn col in dataGridView1.Columns)
                {
                    col.SortMode = DataGridViewColumnSortMode.Automatic;
                    if (col.Name == LedesDocument.XmlRowKeyColumn)
                    {
                        col.Visible = false;
                    }

                    if (doc.Table.Columns.Contains(col.DataPropertyName) && doc.Table.Columns[col.DataPropertyName].ReadOnly)
                    {
                        col.ReadOnly = true;
                        col.DefaultCellStyle.BackColor = SystemColors.Control;
                    }

                    LedesField rule = xml ? null : LedesFormat.FindFieldRule(col.Name);
                    if (rule != null)
                    {
                        col.ToolTipText = col.Name + " (" + rule.Type + (rule.Required ? ", required" : "") +
                                          (rule.MaxLength > 0 ? ", max " + rule.MaxLength : "") + ")";
                    }
                }

                dataGridView1.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);

                PopulateFormatList(doc);
                LEDESoutput.Text = string.Empty;
                dirty = false;
                rowsChanged = false;
                tc1.SelectedTab = tabPage1;
            }
            finally
            {
                loading = false;
            }

            RunValidation(false);
            UpdateTitle();
        }

        private string displayLabel;

        private void RunValidation(bool showTab)
        {
            if (document == null)
            {
                return;
            }

            try
            {
                CommitGridEdits();

                issues = new List<LedesIssue>();

                // Row numbers in load-time issues go stale once rows are added or removed.
                issues.AddRange(document.ParseIssues.Where(i => !rowsChanged || i.RowIndex < 0));
                issues.AddRange(LedesValidator.Validate(document));

                ShowIssues();

                int errors = issues.Count(i => i.Severity == IssueSeverity.Error);
                int warnings = issues.Count(i => i.Severity == IssueSeverity.Warning);

                if (showTab)
                {
                    tc1.SelectedTab = tabIssues;
                    SetStatus(errors == 0 && warnings == 0
                        ? "No problems found."
                        : "Found " + errors + " error(s) and " + warnings + " warning(s). Double-click one to jump to it.");
                }
            }
            catch (Exception ex)
            {
                ErrorReporter.Show(this, "validate the file", ex);
            }
        }

        private void ShowIssues()
        {
            lvIssues.BeginUpdate();
            try
            {
                lvIssues.Items.Clear();
                foreach (LedesIssue issue in issues.OrderByDescending(i => i.Severity).ThenBy(i => i.RowIndex))
                {
                    ListViewItem item = new ListViewItem(issue.Severity.ToString());
                    item.SubItems.Add(issue.RowIndex >= 0 ? (issue.RowIndex + 1).ToString() : "-");
                    item.SubItems.Add(issue.Column ?? string.Empty);
                    item.SubItems.Add(issue.Message);
                    item.Tag = issue;
                    item.ForeColor = issue.Severity == IssueSeverity.Error ? Color.Firebrick
                                   : issue.Severity == IssueSeverity.Warning ? Color.DarkGoldenrod
                                   : SystemColors.WindowText;
                    lvIssues.Items.Add(item);
                }
            }
            finally
            {
                lvIssues.EndUpdate();
            }

            // Flag the cells themselves so problems are visible in the editor.
            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                row.ErrorText = string.Empty;
                foreach (DataGridViewCell cell in row.Cells)
                {
                    cell.ErrorText = string.Empty;
                }
            }

            foreach (LedesIssue issue in issues.Where(i => i.RowIndex >= 0))
            {
                DataGridViewRow row = FindGridRow(issue.RowIndex);
                if (row == null)
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(issue.Column) && dataGridView1.Columns.Contains(issue.Column))
                {
                    DataGridViewCell cell = row.Cells[issue.Column];
                    cell.ErrorText = string.IsNullOrEmpty(cell.ErrorText) ? issue.Message : cell.ErrorText + Environment.NewLine + issue.Message;
                }
                else
                {
                    row.ErrorText = string.IsNullOrEmpty(row.ErrorText) ? issue.Message : row.ErrorText + Environment.NewLine + issue.Message;
                }
            }

            int errors = issues.Count(i => i.Severity == IssueSeverity.Error);
            int warnings = issues.Count(i => i.Severity == IssueSeverity.Warning);
            tabIssues.Text = issues.Count == 0 ? "Issues" : "Issues (" + errors + " errors, " + warnings + " warnings)";
            UpdateCounts();
        }

        private void SaveAs()
        {
            if (document == null)
            {
                return;
            }

            RunValidation(false);
            int errors = issues.Count(i => i.Severity == IssueSeverity.Error);
            if (errors > 0)
            {
                DialogResult answer = MessageBox.Show(this,
                    "The file has " + errors + " error(s). E-billing systems may reject it." + Environment.NewLine + Environment.NewLine +
                    "Save anyway?",
                    "Save with errors", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);

                if (answer != DialogResult.Yes)
                {
                    tc1.SelectedTab = tabIssues;
                    return;
                }
            }

            using (SaveFileDialog dlg = new SaveFileDialog())
            {
                dlg.Title = "Save LEDES file";
                if (document.IsXml)
                {
                    dlg.Filter = "LEDES XML (*.xml)|*.xml|All files (*.*)|*.*";
                    dlg.DefaultExt = "xml";
                }
                else
                {
                    dlg.Filter = document.Format.Name + " file (*.LED)|*.LED|Text file (*.txt)|*.txt|All files (*.*)|*.*";
                    dlg.DefaultExt = "LED";
                }

                dlg.AddExtension = true;
                dlg.OverwritePrompt = true;
                dlg.FileName = document.FilePath != null ? Path.GetFileName(document.FilePath) : SuggestedFileName();
                SetInitialDirectory(dlg);

                if (dlg.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                try
                {
                    Cursor = Cursors.WaitCursor;
                    LedesWriter.Save(document, dlg.FileName);
                    dirty = false;
                    displayLabel = Path.GetFileName(dlg.FileName);
                    UpdateTitle();
                    SetStatus("Saved " + Path.GetFileName(dlg.FileName) + ".");
                }
                catch (Exception ex)
                {
                    ErrorReporter.Show(this, "save " + SafeFileName(dlg.FileName), ex);
                }
                finally
                {
                    Cursor = Cursors.Default;
                }
            }
        }

        private void ShowHelp()
        {
            try
            {
                using (HelpForm help = new HelpForm())
                {
                    help.ShowDialog(this);
                }
            }
            catch (Exception ex)
            {
                ErrorReporter.Show(this, "open help", ex);
            }
        }

        // ---- Helpers ------------------------------------------------------------------------------

        /// <summary>Pushes any in-progress cell edit into the table and settles row indexes.</summary>
        private void CommitGridEdits()
        {
            if (document == null)
            {
                return;
            }

            if (dataGridView1.IsCurrentCellInEditMode)
            {
                dataGridView1.EndEdit();
            }

            BindingManagerBase bm = BindingContext[dataGridView1.DataSource ?? document.Table];
            if (bm != null)
            {
                bm.EndCurrentEdit();
            }

            // Removes deleted rows from the table so issue row numbers match the grid.
            loading = true;
            try
            {
                document.Table.AcceptChanges();
            }
            finally
            {
                loading = false;
            }
        }

        private DataGridViewRow FindGridRow(int tableIndex)
        {
            if (document == null || tableIndex < 0 || tableIndex >= document.Table.Rows.Count)
            {
                return null;
            }

            DataRow target = document.Table.Rows[tableIndex];
            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                DataRowView view = row.DataBoundItem as DataRowView;
                if (view != null && view.Row == target)
                {
                    return row;
                }
            }

            return null;
        }

        private void PopulateFormatList(LedesDocument doc)
        {
            suppressFormatEvent = true;
            try
            {
                cboFormat.Items.Clear();
                if (doc.IsXml)
                {
                    cboFormat.Items.Add(doc.Format);
                    cboFormat.Enabled = false;
                }
                else
                {
                    foreach (LedesFormat f in LedesFormat.ConvertTargets)
                    {
                        cboFormat.Items.Add(f);
                    }

                    if (!cboFormat.Items.Contains(doc.Format))
                    {
                        cboFormat.Items.Insert(0, doc.Format);
                    }

                    cboFormat.Enabled = true;
                }

                cboFormat.SelectedItem = doc.Format;
            }
            finally
            {
                suppressFormatEvent = false;
            }
        }

        private void SelectFormat(LedesFormat format)
        {
            suppressFormatEvent = true;
            try
            {
                cboFormat.SelectedItem = format;
            }
            finally
            {
                suppressFormatEvent = false;
            }
        }

        private bool ConfirmDiscardChanges()
        {
            if (!dirty)
            {
                return true;
            }

            DialogResult answer = MessageBox.Show(this,
                "You have unsaved changes. Save them first?",
                "Unsaved changes", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);

            if (answer == DialogResult.Cancel)
            {
                return false;
            }

            if (answer == DialogResult.Yes)
            {
                SaveAs();
                return !dirty; // Still dirty means the save was cancelled or failed.
            }

            return true;
        }

        private void MarkDirty()
        {
            if (!dirty)
            {
                dirty = true;
                UpdateTitle();
            }

            UpdateCounts();
        }

        private void UpdateTitle()
        {
            string name = DisplayName(document) ?? "Untitled";
            string format = document != null ? " [" + document.Format.Name + "]" : string.Empty;
            Text = "LEDES Editor - " + name + format + (dirty ? " *" : string.Empty);
        }

        private string DisplayName(LedesDocument doc)
        {
            if (!string.IsNullOrEmpty(displayLabel))
            {
                return displayLabel;
            }

            return doc != null && doc.FilePath != null ? Path.GetFileName(doc.FilePath) : null;
        }

        private void UpdateCounts()
        {
            if (document == null)
            {
                lblCounts.Text = string.Empty;
                return;
            }

            int rows = document.Table.Rows.Cast<DataRow>().Count(r => r.RowState != DataRowState.Deleted);
            int invoices = document.Table.Columns.Contains("INVOICE_NUMBER")
                ? document.Table.Rows.Cast<DataRow>()
                    .Where(r => r.RowState != DataRowState.Deleted)
                    .Select(r => Convert.ToString(r["INVOICE_NUMBER"]))
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Distinct()
                    .Count()
                : 0;

            lblCounts.Text = rows + " line item(s)" + (invoices > 0 ? ", " + invoices + " invoice(s)" : string.Empty);
        }

        private void SetStatus(string text)
        {
            lblStatus.Text = text ?? string.Empty;
        }

        private string SuggestedFileName()
        {
            string stem = "invoice";
            if (document != null && document.Table.Columns.Contains("INVOICE_NUMBER") && document.Table.Rows.Count > 0)
            {
                string inv = Convert.ToString(document.Table.Rows[0]["INVOICE_NUMBER"]);
                if (!string.IsNullOrWhiteSpace(inv))
                {
                    stem = new string(inv.Trim().Select(ch => Path.GetInvalidFileNameChars().Contains(ch) ? '_' : ch).ToArray());
                }
            }

            return stem + (document != null && document.IsXml ? ".xml" : ".LED");
        }

        private void SetInitialDirectory(FileDialog dlg)
        {
            try
            {
                string dir = document != null && document.FilePath != null ? Path.GetDirectoryName(document.FilePath) : null;
                dlg.InitialDirectory = !string.IsNullOrEmpty(dir) && Directory.Exists(dir)
                    ? dir
                    : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            }
            catch (Exception)
            {
                // Fall back to the dialog's own default.
            }
        }

        private static string SafeFileName(string path)
        {
            try
            {
                return string.IsNullOrEmpty(path) ? "the file" : Path.GetFileName(path);
            }
            catch (ArgumentException)
            {
                return "the file";
            }
        }

        private static LedesDocument BlankDocument(LedesFormat format)
        {
            LedesDocument doc = new LedesDocument();
            doc.Format = format;
            doc.Identifier = format.Identifier;
            doc.Table = LedesDocument.CreateTable(format.Fields.Select(f => f.Name));
            return doc;
        }
    }
}
