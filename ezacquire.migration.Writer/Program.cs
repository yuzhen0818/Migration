using ezLib.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;

namespace ezacquire.migration.Writer
{
    static class Program
    {
        /// <summary>
        /// 應用程式的主要進入點。
        /// </summary>
        [STAThread]
        static void Main()
        {
            Trace.Listeners.Add(new LogFileWriterListener("ezacquire.migration.Writer"));
            LogCleaner.Clear();

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new WriterWindow());
        }
    }
}
