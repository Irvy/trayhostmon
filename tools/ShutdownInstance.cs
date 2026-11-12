using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

// Build-time helper: stops a running TrayHostMon instance so build.cmd can
// overwrite its exe without a "file in use" failure — but only when that
// instance was launched from the build directory.
//
// Windows has no SIGTERM for a windowless GUI app, so the app listens on a
// named EventWaitHandle ("Local\TrayHostMon.Shutdown") and exits cleanly when
// it is set. This helper finds the process, checks its exe path is the one
// being rebuilt, sets the event, and waits for it to exit.
//
// Kept in tools\ on purpose so build.cmd's `*.cs` glob does not compile it into
// the app. Standalone: no project deps.
//
//   shutdown.exe
//     Exit 0  = nothing to stop, not our instance, or stopped cleanly
//     Exit 1  = our instance was found but did not exit in time
static class ShutdownInstance
{
    private const string EventName = "Local\\TrayHostMon.Shutdown";
    private const int WaitMs = 10000;

    private static int Main()
    {
        Process[] procs = Process.GetProcessesByName("TrayHostMon");
        if (procs.Length == 0)
        {
            Console.WriteLine("No running TrayHostMon instance.");
            return 0;
        }

        // The exe build.cmd is about to overwrite lives in the current directory.
        string buildExe = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, "TrayHostMon.exe"));

        foreach (Process p in procs)
        {
            string runningPath = null;
            try
            {
                runningPath = p.MainModule.FileName;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Warning: could not read path of process " + p.Id +
                    " (" + ex.Message + "); leaving it alone.");
                continue;
            }

            if (!string.Equals(Path.GetFullPath(runningPath), buildExe, StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("Running instance is not from the build dir (" + runningPath +
                    "); leaving it alone.");
                continue;
            }

            Console.WriteLine("Stopping build-dir instance (pid " + p.Id + ")...");
            try
            {
                using (EventWaitHandle ev = EventWaitHandle.OpenExisting(EventName))
                {
                    ev.Set();
                }
            }
            catch (WaitHandleCannotBeOpenedException)
            {
                Console.Error.WriteLine(
                    "The running instance did not register its shutdown event. It was likely " +
                    "built before v1.0.3 — close it manually and try again.");
                return 1;
            }

            if (!p.WaitForExit(WaitMs))
            {
                Console.Error.WriteLine("Instance did not exit within " + (WaitMs / 1000) +
                    "s; close it manually and try again.");
                return 1;
            }
            Console.WriteLine("Stopped.");
            return 0;
        }

        Console.WriteLine("No build-dir instance found.");
        return 0;
    }
}
