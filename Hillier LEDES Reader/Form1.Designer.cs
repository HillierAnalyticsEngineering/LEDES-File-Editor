
namespace Hillier_LEDES_Reader
{
    partial class LEDESeditor
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(LEDESeditor));
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.btnLoad = new System.Windows.Forms.ToolStripButton();
            this.btnExample = new System.Windows.Forms.ToolStripDropDownButton();
            this.mnuExample1998B = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuExample1998BIV2 = new System.Windows.Forms.ToolStripMenuItem();
            this.sep1 = new System.Windows.Forms.ToolStripSeparator();
            this.btnValidate = new System.Windows.Forms.ToolStripButton();
            this.btnPreview = new System.Windows.Forms.ToolStripButton();
            this.btnWrite = new System.Windows.Forms.ToolStripButton();
            this.sep2 = new System.Windows.Forms.ToolStripSeparator();
            this.lblFormat = new System.Windows.Forms.ToolStripLabel();
            this.cboFormat = new System.Windows.Forms.ToolStripComboBox();
            this.btnClose = new System.Windows.Forms.ToolStripButton();
            this.btnHelp = new System.Windows.Forms.ToolStripButton();
            this.tc1 = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.dataGridView1 = new System.Windows.Forms.DataGridView();
            this.tabIssues = new System.Windows.Forms.TabPage();
            this.lvIssues = new System.Windows.Forms.ListView();
            this.colSeverity = new System.Windows.Forms.ColumnHeader();
            this.colRow = new System.Windows.Forms.ColumnHeader();
            this.colField = new System.Windows.Forms.ColumnHeader();
            this.colMessage = new System.Windows.Forms.ColumnHeader();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.LEDESoutput = new System.Windows.Forms.TextBox();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.lblStatus = new System.Windows.Forms.ToolStripStatusLabel();
            this.lblCounts = new System.Windows.Forms.ToolStripStatusLabel();
            this.bindingSource1 = new System.Windows.Forms.BindingSource(this.components);
            this.toolStrip1.SuspendLayout();
            this.tc1.SuspendLayout();
            this.tabPage1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).BeginInit();
            this.tabIssues.SuspendLayout();
            this.tabPage2.SuspendLayout();
            this.statusStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.bindingSource1)).BeginInit();
            this.SuspendLayout();
            //
            // toolStrip1
            //
            this.toolStrip1.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            this.toolStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.btnLoad,
            this.btnExample,
            this.sep1,
            this.btnValidate,
            this.btnPreview,
            this.btnWrite,
            this.sep2,
            this.lblFormat,
            this.cboFormat,
            this.btnClose,
            this.btnHelp});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Padding = new System.Windows.Forms.Padding(8, 4, 8, 4);
            this.toolStrip1.Size = new System.Drawing.Size(1343, 36);
            this.toolStrip1.TabIndex = 0;
            //
            // btnLoad
            //
            this.btnLoad.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.btnLoad.Name = "btnLoad";
            this.btnLoad.Text = "Open...";
            this.btnLoad.ToolTipText = "Open a LEDES file (Ctrl+O)";
            this.btnLoad.Click += new System.EventHandler(this.btnLoad_Click);
            //
            // btnExample
            //
            this.btnExample.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.btnExample.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.mnuExample1998B,
            this.mnuExample1998BIV2});
            this.btnExample.Name = "btnExample";
            this.btnExample.Text = "Example";
            this.btnExample.ToolTipText = "Load a sample LEDES invoice";
            //
            // mnuExample1998B
            //
            this.mnuExample1998B.Name = "mnuExample1998B";
            this.mnuExample1998B.Text = "LEDES 1998B example";
            this.mnuExample1998B.Click += new System.EventHandler(this.mnuExample1998B_Click);
            //
            // mnuExample1998BIV2
            //
            this.mnuExample1998BIV2.Name = "mnuExample1998BIV2";
            this.mnuExample1998BIV2.Text = "LEDES 1998BI V2 example";
            this.mnuExample1998BIV2.Click += new System.EventHandler(this.mnuExample1998BIV2_Click);
            //
            // btnValidate
            //
            this.btnValidate.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.btnValidate.Name = "btnValidate";
            this.btnValidate.Text = "Validate";
            this.btnValidate.ToolTipText = "Check the file for LEDES errors (F5)";
            this.btnValidate.Click += new System.EventHandler(this.btnValidate_Click);
            //
            // btnPreview
            //
            this.btnPreview.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.btnPreview.Name = "btnPreview";
            this.btnPreview.Text = "Preview Output";
            this.btnPreview.ToolTipText = "Show the text that will be saved (Ctrl+P)";
            this.btnPreview.Click += new System.EventHandler(this.btnPreview_Click);
            //
            // btnWrite
            //
            this.btnWrite.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.btnWrite.Name = "btnWrite";
            this.btnWrite.Text = "Save As...";
            this.btnWrite.ToolTipText = "Write the LEDES file (Ctrl+S)";
            this.btnWrite.Click += new System.EventHandler(this.btnWrite_Click);
            //
            // lblFormat
            //
            this.lblFormat.Name = "lblFormat";
            this.lblFormat.Text = "Format:";
            //
            // cboFormat
            //
            this.cboFormat.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboFormat.Name = "cboFormat";
            this.cboFormat.Size = new System.Drawing.Size(220, 28);
            this.cboFormat.ToolTipText = "Detected format. Pick another delimited format to convert.";
            this.cboFormat.SelectedIndexChanged += new System.EventHandler(this.cboFormat_SelectedIndexChanged);
            //
            // btnClose
            //
            this.btnClose.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.btnClose.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.btnClose.Name = "btnClose";
            this.btnClose.Text = "Close";
            this.btnClose.Click += new System.EventHandler(this.btnClose_Click);
            //
            // btnHelp
            //
            this.btnHelp.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.btnHelp.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.btnHelp.Name = "btnHelp";
            this.btnHelp.Text = "Help";
            this.btnHelp.ToolTipText = "How to use the editor (F1)";
            this.btnHelp.Click += new System.EventHandler(this.btnHelp_Click);
            //
            // tc1
            //
            this.tc1.Controls.Add(this.tabPage1);
            this.tc1.Controls.Add(this.tabIssues);
            this.tc1.Controls.Add(this.tabPage2);
            this.tc1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tc1.Location = new System.Drawing.Point(0, 36);
            this.tc1.Name = "tc1";
            this.tc1.SelectedIndex = 0;
            this.tc1.Size = new System.Drawing.Size(1343, 600);
            this.tc1.TabIndex = 1;
            //
            // tabPage1
            //
            this.tabPage1.Controls.Add(this.dataGridView1);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Text = "Data Editor";
            this.tabPage1.UseVisualStyleBackColor = true;
            //
            // dataGridView1
            //
            this.dataGridView1.AllowUserToResizeRows = false;
            this.dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dataGridView1.Name = "dataGridView1";
            this.dataGridView1.RowHeadersWidth = 51;
            this.dataGridView1.RowTemplate.Height = 24;
            this.dataGridView1.TabIndex = 0;
            this.dataGridView1.CellValueChanged += new System.Windows.Forms.DataGridViewCellEventHandler(this.dataGridView1_CellValueChanged);
            this.dataGridView1.DataError += new System.Windows.Forms.DataGridViewDataErrorEventHandler(this.dataGridView1_DataError);
            this.dataGridView1.RowsRemoved += new System.Windows.Forms.DataGridViewRowsRemovedEventHandler(this.dataGridView1_RowsRemoved);
            this.dataGridView1.UserAddedRow += new System.Windows.Forms.DataGridViewRowEventHandler(this.dataGridView1_UserAddedRow);
            //
            // tabIssues
            //
            this.tabIssues.Controls.Add(this.lvIssues);
            this.tabIssues.Name = "tabIssues";
            this.tabIssues.Padding = new System.Windows.Forms.Padding(3);
            this.tabIssues.Text = "Issues";
            this.tabIssues.UseVisualStyleBackColor = true;
            //
            // lvIssues
            //
            this.lvIssues.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.colSeverity,
            this.colRow,
            this.colField,
            this.colMessage});
            this.lvIssues.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lvIssues.FullRowSelect = true;
            this.lvIssues.HideSelection = false;
            this.lvIssues.MultiSelect = false;
            this.lvIssues.Name = "lvIssues";
            this.lvIssues.TabIndex = 0;
            this.lvIssues.UseCompatibleStateImageBehavior = false;
            this.lvIssues.View = System.Windows.Forms.View.Details;
            this.lvIssues.ItemActivate += new System.EventHandler(this.lvIssues_ItemActivate);
            //
            // colSeverity
            //
            this.colSeverity.Text = "Severity";
            this.colSeverity.Width = 90;
            //
            // colRow
            //
            this.colRow.Text = "Row";
            this.colRow.Width = 60;
            //
            // colField
            //
            this.colField.Text = "Field";
            this.colField.Width = 240;
            //
            // colMessage
            //
            this.colMessage.Text = "Message";
            this.colMessage.Width = 900;
            //
            // tabPage2
            //
            this.tabPage2.Controls.Add(this.LEDESoutput);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage2.Text = "LEDES Output";
            this.tabPage2.UseVisualStyleBackColor = true;
            //
            // LEDESoutput
            //
            this.LEDESoutput.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.LEDESoutput.Dock = System.Windows.Forms.DockStyle.Fill;
            this.LEDESoutput.Font = new System.Drawing.Font("Consolas", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.LEDESoutput.ForeColor = System.Drawing.Color.White;
            this.LEDESoutput.MaxLength = 0;
            this.LEDESoutput.Multiline = true;
            this.LEDESoutput.Name = "LEDESoutput";
            this.LEDESoutput.ReadOnly = true;
            this.LEDESoutput.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.LEDESoutput.TabIndex = 0;
            this.LEDESoutput.WordWrap = false;
            //
            // statusStrip1
            //
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.lblStatus,
            this.lblCounts});
            this.statusStrip1.Location = new System.Drawing.Point(0, 636);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(1343, 26);
            this.statusStrip1.TabIndex = 2;
            //
            // lblStatus
            //
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Spring = true;
            this.lblStatus.Text = "Ready";
            this.lblStatus.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // lblCounts
            //
            this.lblCounts.Name = "lblCounts";
            this.lblCounts.Text = "";
            //
            // LEDESeditor
            //
            this.AllowDrop = true;
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(1343, 662);
            this.Controls.Add(this.tc1);
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.toolStrip1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.KeyPreview = true;
            this.MinimumSize = new System.Drawing.Size(720, 420);
            this.Name = "LEDESeditor";
            this.Text = "LEDES Editor";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.LEDESeditor_FormClosing);
            this.Load += new System.EventHandler(this.LEDESeditor_Load);
            this.DragDrop += new System.Windows.Forms.DragEventHandler(this.LEDESeditor_DragDrop);
            this.DragEnter += new System.Windows.Forms.DragEventHandler(this.LEDESeditor_DragEnter);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.LEDESeditor_KeyDown);
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.tc1.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).EndInit();
            this.tabIssues.ResumeLayout(false);
            this.tabPage2.ResumeLayout(false);
            this.tabPage2.PerformLayout();
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.bindingSource1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripButton btnLoad;
        private System.Windows.Forms.ToolStripDropDownButton btnExample;
        private System.Windows.Forms.ToolStripMenuItem mnuExample1998B;
        private System.Windows.Forms.ToolStripMenuItem mnuExample1998BIV2;
        private System.Windows.Forms.ToolStripSeparator sep1;
        private System.Windows.Forms.ToolStripButton btnValidate;
        private System.Windows.Forms.ToolStripButton btnPreview;
        private System.Windows.Forms.ToolStripButton btnWrite;
        private System.Windows.Forms.ToolStripSeparator sep2;
        private System.Windows.Forms.ToolStripLabel lblFormat;
        private System.Windows.Forms.ToolStripComboBox cboFormat;
        private System.Windows.Forms.ToolStripButton btnClose;
        private System.Windows.Forms.ToolStripButton btnHelp;
        private System.Windows.Forms.TabControl tc1;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.DataGridView dataGridView1;
        private System.Windows.Forms.TabPage tabIssues;
        private System.Windows.Forms.ListView lvIssues;
        private System.Windows.Forms.ColumnHeader colSeverity;
        private System.Windows.Forms.ColumnHeader colRow;
        private System.Windows.Forms.ColumnHeader colField;
        private System.Windows.Forms.ColumnHeader colMessage;
        private System.Windows.Forms.TabPage tabPage2;
        private System.Windows.Forms.TextBox LEDESoutput;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel lblStatus;
        private System.Windows.Forms.ToolStripStatusLabel lblCounts;
        private System.Windows.Forms.BindingSource bindingSource1;
    }
}
