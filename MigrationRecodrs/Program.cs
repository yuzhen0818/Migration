using ezLib.Utility;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MigrationRecodrs
{
    class Program
    {
        static void Main(string[] args)
        {
            Trace.Listeners.Add(new LogFileWriterListener("MigrationRecodrs"));
            LogCleaner.Clear();

            Core core = new Core();
            core.Run();
        }
    }
}
