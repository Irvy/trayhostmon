using System;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;

namespace PingMon
{
    public class HostMonitor
    {
        private readonly HostState _host;
        private CancellationTokenSource _cts;

        public event Action<HostState, State, TimeSpan> StateChanged;

        public HostState Host
        {
            get { return _host; }
        }

        public HostMonitor(HostState host)
        {
            _host = host;
        }

        // Starts this host's loop. Each monitor has its own token, so one host can be stopped alone.
        public void Start(CancellationToken parent)
        {
            Stop();
            _cts = CancellationTokenSource.CreateLinkedTokenSource(parent);
            CancellationToken token = _cts.Token;
            Task.Factory.StartNew(() => RunAsync(token).Wait(), token,
                TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        public void Stop()
        {
            if (_cts != null)
            {
                _cts.Cancel();
                _cts.Dispose();
                _cts = null;
            }
        }

        private async Task RunAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                bool ok = false;
                long rtt = -1;
                try
                {
                    using (var ping = new Ping())
                    {
                        PingReply reply = await ping.SendPingAsync(_host.Cfg.Address, _host.Cfg.TimeoutMs);
                        ok = reply.Status == IPStatus.Success;
                        if (ok) rtt = reply.RoundtripTime;
                    }
                }
                catch (Exception)
                {
                    ok = false;
                }

                if (token.IsCancellationRequested) break;

                _host.LastCheck = DateTime.Now;
                if (ok)
                {
                    _host.FailCount = 0;
                    _host.LastRttMs = rtt;
                    if (_host.State != State.Up) Change(State.Up);
                }
                else
                {
                    _host.FailCount++;
                    _host.LastRttMs = -1;
                    if (_host.FailCount >= _host.Cfg.Threshold && _host.State != State.Down)
                        Change(State.Down);
                }

                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(_host.Cfg.IntervalSec), token);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }
        }

        private void Change(State newState)
        {
            State old = _host.State;
            DateTime now = DateTime.Now;
            TimeSpan duration = now - _host.Since;
            _host.State = newState;
            _host.Since = now;
            if (newState == State.Down) _host.Acked = false;

            var handler = StateChanged;
            if (handler != null) handler(_host, old, duration);
        }
    }
}
