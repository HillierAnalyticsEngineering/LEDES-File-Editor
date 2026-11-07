using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace Hillier_LEDES_Reader
{
    /// <summary>Read-only help window. The text lives in Resources\Help.txt.</summary>
    public class HelpForm : Form
    {
        private const string HelpResource = "Hillier_LEDES_Reader.Resources.Help.txt";

        private readonly TextBox txtHelp;
        private readonly Button btnOk;

        public HelpForm()
        {
            Text = "LEDES Editor Help";
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(760, 640);
            MinimumSize = new Size(420, 320);
            ShowInTaskbar = false;
            MinimizeBox = false;
            KeyPreview = true;

            txtHelp = new TextBox();
            txtHelp.Multiline = true;
            txtHelp.ReadOnly = true;
            txtHelp.ScrollBars = ScrollBars.Vertical;
            txtHelp.WordWrap = true;
            txtHelp.BackColor = SystemColors.Window;
            txtHelp.Font = new Font("Consolas", 10F);
            txtHelp.Dock = DockStyle.Fill;
            txtHelp.Text = LoadHelpText();
            txtHelp.TabStop = false;

            btnOk = new Button();
            btnOk.Text = "Close";
            btnOk.DialogResult = DialogResult.OK;
            btnOk.Anchor = AnchorStyles.Right | AnchorStyles.Bottom;
            btnOk.Size = new Size(96, 30);

            Panel bottom = new Panel();
            bottom.Dock = DockStyle.Bottom;
            bottom.Height = 46;
            bottom.Controls.Add(btnOk);
            btnOk.Location = new Point(bottom.Width - btnOk.Width - 12, 8);

            Controls.Add(txtHelp);
            Controls.Add(bottom);

            AcceptButton = btnOk;
            CancelButton = btnOk;
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            txtHelp.SelectionStart = 0;
            txtHelp.SelectionLength = 0;
            btnOk.Focus();
        }

        internal static string LoadHelpText()
        {
            try
            {
                using (Stream s = Assembly.GetExecutingAssembly().GetManifestResourceStream(HelpResource))
                {
                    if (s != null)
                    {
                        using (StreamReader r = new StreamReader(s))
                        {
                            // TextBox needs Windows line endings.
                            return r.ReadToEnd().Replace("\r\n", "\n").Replace("\n", "\r\n");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorReporter.Log("load help", ex);
            }

            return "Help text is missing from this build." + Environment.NewLine +
                   "See README.md at https://github.com/HillierAnalyticsEngineering/LEDES-File-Editor";
        }
    }
}
