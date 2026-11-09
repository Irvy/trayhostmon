using System;
using System.Collections.Generic;

namespace PingMon
{
    public enum State { Unknown, Up, Down }

    public enum Overall { AllUp, DownAcked, DownUnacked, Paused }

    public class HostState
    {
        public HostConfig Cfg;
        public State State = State.Unknown;
        public DateTime Since = DateTime.Now;
        public DateTime LastCheck = DateTime.MinValue;
        public bool Acked;
        public int FailCount;
        public long LastRttMs = -1;
        public bool Enabled = true;

        public HostState(HostConfig cfg)
        {
            Cfg = cfg;
        }

        // Unknown hosts and disabled hosts are ignored: they don't change the tray icon.
        public static Overall Worst(IEnumerable<HostState> hosts)
        {
            bool anyDownAcked = false;
            foreach (var h in hosts)
            {
                if (!h.Enabled) continue;
                if (h.State == State.Down)
                {
                    if (!h.Acked) return Overall.DownUnacked;
                    anyDownAcked = true;
                }
            }
            return anyDownAcked ? Overall.DownAcked : Overall.AllUp;
        }
    }
}
