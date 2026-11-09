using System;
using System.Collections.Generic;
using System.Threading;
#if !CONSOLE
using System.Windows.Forms;
#endif

namespace PingMon
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
#if CONSOLE
            RunConsole();
#else
            bool createdNew;
            using (var mutex = new Mutex(true, "Local\\pingmon", out createdNew))
            {
                if (!createdNew) return;

                Application.EnableVisualStyles();
                Application.Run(new TrayApp());
                GC.KeepAlive(mutex);
            }
#endif
        }

#if CONSOLE
        private static void RunConsole()
        {
            Config.EnsureExists();
            AppConfig appCfg = Config.Load(Config.IniPath);
            var states = new List<HostState>();
            var monitors = new List<HostMonitor>();
            var cts = new CancellationTokenSource();

            StateLog.Write("pingmon", "START", null);
            Console.WriteLine("pingmon console: " + appCfg.Hosts.Count + " host(s). Press Enter to stop.");
            foreach (var address in appCfg.Hosts)
            {
                var st = new HostState(Config.ToHostConfig(appCfg, address));
                states.Add(st);
                var mon = new HostMonitor(st);
                monitors.Add(mon);
                mon.StateChanged += (h, oldState, duration) =>
                {
                    Console.WriteLine(string.Format("{0} {1} {2}->{3} ({4})",
                        DateTime.Now.ToString("HH:mm:ss"), h.Cfg.Address,
                        oldState, h.State, duration.ToString(@"hh\:mm\:ss")));
                    StateLog.Write(h.Cfg.Address, oldState + "->" + h.State,
                        oldState == State.Unknown ? (TimeSpan?)null : duration);
                };
                mon.Start(cts.Token);
            }

            var timer = new Timer(_ =>
            {
                var parts = new List<string>();
                foreach (var h in states)
                {
                    parts.Add(string.Format("{0}={1}({2}ms)", h.Cfg.Address, h.State,
                        h.LastRttMs < 0 ? "-" : h.LastRttMs.ToString()));
                }
                Console.WriteLine("  [" + string.Join(" ", parts.ToArray()) + "]");
            }, null, 5000, 5000);

            Console.ReadLine();
            timer.Dispose();
            cts.Cancel();
            foreach (var mon in monitors) mon.Stop();
            StateLog.Write("pingmon", "STOP", null);
        }
#endif
    }
}
