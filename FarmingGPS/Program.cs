using System;
using System.Diagnostics;

namespace FarmingGPS
{
    class Program
    {
        [STAThread]
        static void Main()
        {
            using (Process p = Process.GetCurrentProcess())
                p.PriorityClass = ProcessPriorityClass.High;
            App application = new App();
            application.InitializeComponent();
            application.Run();
        }
    }
}
