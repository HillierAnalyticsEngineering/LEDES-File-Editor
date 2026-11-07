using System;
using System.IO;
using System.Text;
using System.Windows.Forms;
using Hillier_LEDES_Reader.Ledes;

namespace Hillier_LEDES_Reader
{
    /// <summary>Turns exceptions into plain-language messages and keeps a log for bug reports.</summary>
    internal static class ErrorReporter
    {
        private static readonly object LogLock = new object();

        public static string LogPath
        {
            get
            {
                string dir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                if (string.IsNullOrEmpty(dir))
                {
                    dir = Path.GetTempPath();
                }

                return Path.Combine(Path.Combine(dir, "HillierLEDES"), "error.log");
            }
        }

        /// <summary>Shows a friendly message for an error raised by a user action.</summary>
        public static void Show(IWin32Window owner, string action, Exception ex)
        {
            Log(action, ex);

            string message = Describe(ex);
            MessageBoxIcon icon = IsExpected(ex) ? MessageBoxIcon.Warning : MessageBoxIcon.Error;
            if (!IsExpected(ex))
            {
                message += Environment.NewLine + Environment.NewLine + "Details were saved to:" + Environment.NewLine + LogPath;
            }

            try
            {
                MessageBox.Show(owner, message, "Couldn't " + action, MessageBoxButtons.OK, icon);
            }
            catch (Exception)
            {
                // A failing message box must never take the app down.
            }
        }

        /// <summary>Handles exceptions nothing else caught. Returns true when the app can keep running.</summary>
        public static void ShowUnhandled(Exception ex, bool canContinue)
        {
            Log("unhandled", ex);

            string message = "Something unexpected went wrong." + Environment.NewLine + Environment.NewLine +
                             Describe(ex) + Environment.NewLine + Environment.NewLine +
                             "Details were saved to:" + Environment.NewLine + LogPath;

            if (canContinue)
            {
                message += Environment.NewLine + Environment.NewLine + "You can keep working. Save your changes soon.";
            }

            try
            {
                MessageBox.Show(message, "LEDES Editor", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception)
            {
            }
        }

        /// <summary>Plain-language description of the most common failures.</summary>
        public static string Describe(Exception ex)
        {
            if (ex == null)
            {
                return "An unknown error occurred.";
            }

            if (ex is LedesFormatException)
            {
                return ex.Message;
            }

            if (ex is FileNotFoundException || ex is DirectoryNotFoundException)
            {
                return "The file or folder can't be found. It may have been moved, renamed or deleted.";
            }

            if (ex is UnauthorizedAccessException)
            {
                return "Windows denied access to the file. It may be read-only, or you may not have permission for that folder.";
            }

            if (ex is PathTooLongException)
            {
                return "The file path is too long. Move the file to a shorter folder path and try again.";
            }

            IOException io = ex as IOException;
            if (io != null)
            {
                // 0x20 sharing violation, 0x21 lock violation.
                int code = io.HResult & 0xFFFF;
                if (code == 0x20 || code == 0x21)
                {
                    return "Another program has the file open. Close it there and try again.";
                }

                if (code == 0x70 || code == 0x27)
                {
                    return "The disk is full.";
                }

                return "The file couldn't be read or written: " + io.Message;
            }

            if (ex is OutOfMemoryException)
            {
                return "The file is too large to work with on this computer.";
            }

            if (ex is InvalidOperationException || ex is ArgumentException)
            {
                return ex.Message;
            }

            return ex.GetType().Name + ": " + ex.Message;
        }

        public static void Log(string action, Exception ex)
        {
            try
            {
                lock (LogLock)
                {
                    string path = LogPath;
                    Directory.CreateDirectory(Path.GetDirectoryName(path));

                    // Keep the log from growing forever.
                    FileInfo fi = new FileInfo(path);
                    if (fi.Exists && fi.Length > 1024 * 1024)
                    {
                        File.Delete(path);
                    }

                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine("==== " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " | " + action);
                    sb.AppendLine(ex == null ? "(no exception)" : ex.ToString());
                    File.AppendAllText(path, sb.ToString(), Encoding.UTF8);
                }
            }
            catch (Exception)
            {
                // Logging is best effort.
            }
        }

        private static bool IsExpected(Exception ex)
        {
            return ex is LedesFormatException || ex is IOException || ex is UnauthorizedAccessException ||
                   ex is ArgumentException;
        }
    }
}
