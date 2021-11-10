using ezLib.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;

namespace ezacquire.migration.Extractor
{
    static class Program
    {
        /// <summary>
        /// 應用程式的主要進入點。
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            Trace.Listeners.Add(new LogFileWriterListener("ezacquire.migration.Extractor"));
            LogCleaner.Clear();

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new ExtractorWindow(args));
        }
    }
}
