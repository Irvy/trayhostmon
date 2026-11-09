using System;
using System.IO;
using System.Text;

namespace PingMon
{
    public static class StateLog
    {
        public static readonly string Path =
            System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "pingmon.log");

        private static readonly object Gate = new object();
        private const string Header = "timestamp,host,transition,duration";

        public static void Write(string host, string transition, TimeSpan? duration)
        {
            string name = host.IndexOf(',') >= 0 ? "\"" + host.Replace("\"", "\"\"") + "\"" : host;
            string dur = duration.HasValue ? FormatDuration(duration.Value) : "";
            string line = string.Format("{0},{1},{2},{3}{4}",
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), name, transition, dur, Environment.NewLine);

            lock (Gate)
            {
                bool isNew = !File.Exists(Path);
                var sb = new StringBuilder();
                if (isNew) sb.Append(Header).Append(Environment.NewLine);
                sb.Append(line);
                File.AppendAllText(Path, sb.ToString(), new UTF8Encoding(false));
            }
        }

        private static string FormatDuration(TimeSpan t)
        {
            return string.Format("{0}.{1:00}:{2:00}:{3:00}", t.Days, t.Hours, t.Minutes, t.Seconds);
        }
    }
}
