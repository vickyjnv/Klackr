using System;
using System.Threading;
using System.Windows.Forms;
using Klackr.UI;

namespace Klackr
{
    internal static class Program
    {
        private const string MutexName = "Klackr_SingleInstance_Mutex";

        [STAThread]
        private static void Main()
        {
            using var mutex = new Mutex(true, MutexName, out bool createdNew);
            if (!createdNew)
            {
                // Another instance is already running
                return;
            }

            Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Application.Run(new TrayApplicationContext());
        }
    }
}
