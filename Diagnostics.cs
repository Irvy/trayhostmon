#if DEBUG_MEM
using System;
using System.Diagnostics;

namespace PingMon
{
    // Debug-only memory figures. Compiled only by 'build.cmd debug'.
    public static class Diagnostics
    {
        public static string Snapshot()
        {
            Process p = Process.GetCurrentProcess();
            p.Refresh();
            return string.Format(
                "private {0:N1} MB, working set {1:N1} MB\n.NET heap {2:N1} MB\ngen0 {3}, gen1 {4}, gen2 {5} collections\nthreads {6}, handles {7}",
                p.PrivateMemorySize64 / 1048576.0,
                p.WorkingSet64 / 1048576.0,
                GC.GetTotalMemory(false) / 1048576.0,
                GC.CollectionCount(0), GC.CollectionCount(1), GC.CollectionCount(2),
                p.Threads.Count, p.HandleCount);
        }

        // Full cleanup, returning the figures before and after.
        public static string ForceCleanup()
        {
            string before = Snapshot();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            string after = Snapshot();
            return "BEFORE:\n" + before + "\n\nAFTER:\n" + after;
        }
    }
}
#endif
