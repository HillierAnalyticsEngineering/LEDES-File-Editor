using System;
using System.Threading;
using System.Windows.Forms;

namespace Hillier_LEDES_Reader
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            // Route every unhandled exception to a friendly message and a log entry instead of a crash dialog.
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += OnThreadException;
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

            try
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                // A file passed on the command line (or dropped on the .exe) opens instead of the example.
                string startFile = args != null && args.Length > 0 ? args[0] : null;
                Application.Run(new LEDESeditor(startFile));
            }
            catch (Exception ex)
            {
                ErrorReporter.ShowUnhandled(ex, false);
            }
        }

        private static void OnThreadException(object sender, ThreadExceptionEventArgs e)
        {
            ErrorReporter.ShowUnhandled(e.Exception, true);
        }

        private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            ErrorReporter.ShowUnhandled(e.ExceptionObject as Exception, false);
        }
    }
}
