using System;
using System.Threading;
using System.Windows.Forms;

namespace TrayHostMon
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            bool createdNew;
            using (var mutex = new Mutex(true, "Local\\TrayHostMon", out createdNew))
            {
                if (!createdNew) return;

                Application.EnableVisualStyles();
                Application.Run(new TrayApp());
                GC.KeepAlive(mutex);
            }
        }
    }
}
