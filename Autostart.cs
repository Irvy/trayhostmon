using System;
using System.IO;
using System.Runtime.InteropServices;

namespace TrayHostMon
{
    public static class Autostart
    {
        public static readonly string LinkPath =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), "TrayHostMon.lnk");

        public static bool IsEnabled
        {
            get { return File.Exists(LinkPath); }
        }

        public static void Enable()
        {
            string exe = System.Reflection.Assembly.GetExecutingAssembly().Location;
            Type shellType = Type.GetTypeFromProgID("WScript.Shell");
            object shell = Activator.CreateInstance(shellType);
            try
            {
                object link = shellType.InvokeMember("CreateShortcut",
                    System.Reflection.BindingFlags.InvokeMethod, null, shell, new object[] { LinkPath });
                try
                {
                    Type linkType = link.GetType();
                    linkType.InvokeMember("TargetPath",
                        System.Reflection.BindingFlags.SetProperty, null, link, new object[] { exe });
                    linkType.InvokeMember("WorkingDirectory",
                        System.Reflection.BindingFlags.SetProperty, null, link,
                        new object[] { Path.GetDirectoryName(exe) });
                    linkType.InvokeMember("Save",
                        System.Reflection.BindingFlags.InvokeMethod, null, link, null);
                }
                finally
                {
                    Marshal.ReleaseComObject(link);
                }
            }
            finally
            {
                Marshal.ReleaseComObject(shell);
            }
        }

        public static void Disable()
        {
            if (File.Exists(LinkPath)) File.Delete(LinkPath);
        }
    }
}
