using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

namespace TrayHostMon
{
    public class TrayApp : ApplicationContext
    {
        private readonly NotifyIcon _tray;
        private readonly ContextMenuStrip _menu;
        private readonly ToolStripMenuItem _ackItem;
        private readonly ToolStripMenuItem _autostartItem;
        private readonly ToolStripMenuItem _pauseItem;
        private readonly List<HostState> _states = new List<HostState>();
        private readonly Dictionary<HostState, HostMonitor> _monitors = new Dictionary<HostState, HostMonitor>();
        private readonly SynchronizationContext _ui;
        private readonly CancellationTokenSource _lifetime = new CancellationTokenSource();
        private AppConfig _config = new AppConfig();
        private Overall _overall = Overall.AllUp;
        private WidgetForm _widget;
        private DateTime _widgetClosedAt = DateTime.MinValue;
        private AddHostForm _addForm;
        private bool _paused;
        private bool _exited;

        public TrayApp()
        {
            _ui = SynchronizationContext.Current ?? new SynchronizationContext();

            _ackItem = new ToolStripMenuItem("Acknowledge all", null, (s, e) => AcknowledgeAll());
            _pauseItem = new ToolStripMenuItem("Pause monitoring", null, (s, e) => TogglePause());
            _autostartItem = new ToolStripMenuItem("Start with Windows", null, (s, e) => ToggleAutostart());
            var clearLogItem = new ToolStripMenuItem("Clear log", null, (s, e) => ClearLog());
            _menu = new ContextMenuStrip();
            _menu.Items.Add(_ackItem);
            _menu.Items.Add(_pauseItem);
            _menu.Items.Add("Add host...", null, (s, e) => AddHost());
            _menu.Items.Add("Open log", null, (s, e) => OpenLog());
            _menu.Items.Add(clearLogItem);
            _menu.Items.Add("Reload config", null, (s, e) => ReloadConfig());
            _menu.Items.Add(new ToolStripSeparator());
            _menu.Items.Add(_autostartItem);
            var aboutItem = new ToolStripMenuItem("About...", null, (s, e) => AboutClicked());
#if DEBUG_MEM
            aboutItem.MouseUp += (s, e) => AboutMouseUp(e);
#endif
            _menu.Items.Add(aboutItem);
            _menu.Items.Add("Exit", null, (s, e) => ExitApp());
            _menu.Opening += (s, e) =>
            {
                _ackItem.Enabled = !_paused && HasUnackedDown();
                _pauseItem.Text = _paused ? "Resume monitoring" : "Pause monitoring";
                _autostartItem.Checked = Autostart.IsEnabled;
                clearLogItem.Enabled = File.Exists(StateLog.Path);
            };

            _tray = new NotifyIcon
            {
                ContextMenuStrip = _menu,
                Visible = true
            };
            _tray.MouseClick += (s, e) =>
            {
                if (e.Button == MouseButtons.Left) ToggleWidget();
            };

            SystemEvents.SessionEnding += OnSessionEnding;

            Config.EnsureExists();
            StateLog.Write("TrayHostMon", "START", null);
            LoadAndStart();
            if (_config.Skipped.Count > 0) ShowSkippedNotice();
        }

        private void OnSessionEnding(object sender, SessionEndingEventArgs e)
        {
            ExitApp();
        }

        // Reads the file into running values and starts a monitor per host.
        private void LoadAndStart()
        {
            _config = Config.Load(Config.IniPath);
            _states.Clear();
            foreach (var hostSpec in _config.Hosts)
            {
                var st = new HostState(Config.ToHostConfig(_config, hostSpec));
                st.Enabled = !Config.IsDisabled(hostSpec);
                _states.Add(st);
                if (st.Enabled)
                    StartMonitor(st);
            }
            Refresh();
        }

        // Starts (or restarts) one host's monitor. Does nothing while paused or disabled.
        private void StartMonitor(HostState st)
        {
            var mon = new HostMonitor(st);
            mon.StateChanged += OnStateChanged;
            _monitors[st] = mon;
            if (!_paused && st.Enabled) mon.Start(_lifetime.Token);
        }

        private void StopMonitor(HostState st)
        {
            HostMonitor mon;
            if (_monitors.TryGetValue(st, out mon))
            {
                mon.Stop();
                _monitors.Remove(st);
            }
        }

        private void StopMonitors()
        {
            foreach (var mon in _monitors.Values) mon.Stop();
        }

        // Writes the running values to TrayHostMon.ini.
        private void SaveConfig()
        {
            try
            {
                _config.Hosts = _states.Select(h => Config.MakeHostSpec(h.Cfg.Address, h.Enabled)).ToList();
                Config.Save(Config.IniPath, _config);
            }
            catch (Exception ex)
            {
                ShowBalloon("Could not save config: " + ex.Message);
            }
        }

        private void ShowSkippedNotice()
        {
            ShowBalloon("Ignored config line(s): " + string.Join(", ", _config.Skipped.ToArray()));
        }

        // Called on a background thread: marshal to the UI thread.
        private void OnStateChanged(HostState h, State oldState, TimeSpan duration)
        {
            _ui.Post(_ =>
            {
                StateLog.Write(h.Cfg.Address, oldState + "->" + h.State,
                    oldState == State.Unknown ? (TimeSpan?)null : duration);
                Refresh();
            }, null);
        }

        private void Refresh()
        {
            _overall = _paused ? Overall.Paused : HostState.Worst(_states);
            _tray.Icon = IconFactory.Get(_overall);
            _tray.Text = Tooltip();
            if (_widget != null && !_widget.IsDisposed) _widget.Invalidate();
        }

        private string Tooltip()
        {
            if (_paused) return "TrayHostMon: paused";
            var enabledStates = _states.Where(h => h.Enabled).ToList();
            int up = enabledStates.Count(h => h.State == State.Up);
            string text = string.Format("TrayHostMon: {0}/{1} up", up, enabledStates.Count);
            return text.Length > 63 ? text.Substring(0, 63) : text;
        }

        private void TogglePause()
        {
            if (_paused) ResumeMonitoring();
            else PauseMonitoring();
        }

        private void PauseMonitoring()
        {
            StopMonitors();
            _paused = true;
            StateLog.Write("TrayHostMon", "PAUSE", null);
            Refresh();
        }

        private void ResumeMonitoring()
        {
            _paused = false;
            StateLog.Write("TrayHostMon", "RESUME", null);
            // Restart every enabled monitor, resetting the fail counts.
            foreach (var h in _states) h.FailCount = 0;
            foreach (var h in _states.ToList())
            {
                if (!h.Enabled) continue;
                StopMonitor(h);
                StartMonitor(h);
            }
            Refresh();
        }

        private bool HasUnackedDown()
        {
            return _states.Any(h => h.State == State.Down && !h.Acked);
        }

        private void AcknowledgeAll()
        {
            foreach (var h in _states)
                if (h.State == State.Down) h.Acked = true;
            Refresh();
        }

        private void OpenLog()
        {
            if (!File.Exists(StateLog.Path)) return;
            Process.Start(StateLog.Path);
        }

        private void ClearLog()
        {
            try
            {
                if (File.Exists(StateLog.Path))
                    File.Delete(StateLog.Path);
                StateLog.Write("TrayHostMon", "LOG CLEARED", null);
                ShowBalloon("Log cleared");
            }
            catch (Exception ex)
            {
                ShowBalloon("Could not clear log: " + ex.Message);
            }
        }

        // Reads TrayHostMon.ini without writing it. Keeps state for hosts still listed.
        private void ReloadConfig()
        {
            AppConfig fresh;
            try
            {
                fresh = Config.Load(Config.IniPath);
            }
            catch (Exception ex)
            {
                ShowBalloon("Config error: " + ex.Message);
                return;
            }
            if (fresh.Hosts.Count == 0)
            {
                ShowBalloon("Config has no valid hosts; keeping current config.");
                return;
            }

            var old = new List<HostState>(_states);
            foreach (var h in old) StopMonitor(h);
            _states.Clear();

            _config = fresh;
            foreach (var hostSpec in fresh.Hosts)
            {
                string address = Config.GetAddress(hostSpec);
                bool enabled = !Config.IsDisabled(hostSpec);
                HostState keep = old.Find(h => string.Equals(h.Cfg.Address, address, StringComparison.OrdinalIgnoreCase));
                var st = keep ?? new HostState(Config.ToHostConfig(fresh, hostSpec));
                st.Cfg = Config.ToHostConfig(fresh, hostSpec);
                st.Enabled = enabled;
                _states.Add(st);
                if (enabled) StartMonitor(st);
            }
            Refresh();

            ShowBalloon("Config reloaded: " + fresh.Hosts.Count + " host(s)." +
                (fresh.Skipped.Count > 0 ? " Ignored: " + string.Join(", ", fresh.Skipped.ToArray()) : ""));
        }

        // Adds a host to the running list and writes the config. Also allows changing settings.
        private void AddHost()
        {
            if (_addForm != null && !_addForm.IsDisposed)
            {
                _addForm.Activate();
                return;
            }

            _addForm = new AddHostForm(IsListed, _config.IntervalSec, _config.TimeoutMs, _config.Buffer);
            if (_addForm.ShowDialog() != DialogResult.OK)
            {
                _addForm.Dispose();
                _addForm = null;
                return;
            }

            List<string> added = _addForm.Hosts;
            List<string> listed = _addForm.AlreadyListed;
            int? newInterval = _addForm.IntervalSec;
            int? newTimeout = _addForm.TimeoutMs;
            int? newBuffer = _addForm.Buffer;
            _addForm.Dispose();
            _addForm = null;

            // Apply settings changes if any
            bool settingsChanged = false;
            if (newInterval.HasValue && newInterval.Value != _config.IntervalSec)
            {
                _config.IntervalSec = newInterval.Value;
                settingsChanged = true;
            }
            if (newTimeout.HasValue && newTimeout.Value != _config.TimeoutMs)
            {
                _config.TimeoutMs = newTimeout.Value;
                settingsChanged = true;
            }
            if (newBuffer.HasValue && newBuffer.Value != _config.Buffer)
            {
                _config.Buffer = newBuffer.Value;
                settingsChanged = true;
            }

            // If settings changed, update all existing hosts' configs and restart monitors
            if (settingsChanged)
            {
                foreach (var st in _states)
                {
                    st.Cfg = Config.ToHostConfig(_config, Config.MakeHostSpec(st.Cfg.Address, st.Enabled));
                    // Restart monitor to apply new settings
                    HostMonitor mon;
                    if (_monitors.TryGetValue(st, out mon))
                    {
                        mon.Stop();
                        if (!_paused && st.Enabled) mon.Start(_lifetime.Token);
                    }
                }
            }

            // Add new hosts
            foreach (string host in added)
            {
                var st = new HostState(Config.ToHostConfig(_config, host));
                _states.Add(st);
                StartMonitor(st);
                StateLog.Write(host, "ADDED", null);
            }

            SaveConfig();
            Refresh();
            if (_widget != null && !_widget.IsDisposed) _widget.Relayout();

            // Show appropriate message
            if (added.Count > 0 && listed.Count > 0)
                ShowBalloon("Added " + added.Count + " host(s). Already listed: " + string.Join(", ", listed.ToArray()));
            else if (added.Count > 0)
                ShowBalloon("Added " + added.Count + " host(s).");
            else if (settingsChanged)
                ShowBalloon("Settings updated.");
        }

        private bool IsListed(string host)
        {
            return _states.Any(h => string.Equals(h.Cfg.Address, host, StringComparison.OrdinalIgnoreCase));
        }

        private bool _aboutOpen;
#if DEBUG_MEM
        private bool _debugFormOpen;
#endif

        // Simple About box. An invisible topmost owner keeps it in front of other windows.
        private void ShowAbout()
        {
            if (_aboutOpen) return;
            _aboutOpen = true;
            using (var owner = new Form
            {
                StartPosition = FormStartPosition.CenterScreen,
                ShowInTaskbar = false,
                TopMost = true,
                Opacity = 0,
                Size = new Size(0, 0)
            })
            {
                owner.Show();
                MessageBox.Show(owner,
                    "TrayHostMon\r\nA simple tray host ping monitor\r\nMade by Claude/Deepseek/Cline and Irvy in 2026",
                    "About TrayHostMon",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            _aboutOpen = false;
        }

        // About item: a plain click shows the About box. With Shift held (or on a right-click, see
        // AboutMouseUp) the debug build opens the debug form instead.
        private void AboutClicked()
        {
#if DEBUG_MEM
            if ((Control.ModifierKeys & Keys.Shift) == Keys.Shift)
            {
                ShowDebugForm();
                return;
            }
#endif
            ShowAbout();
        }

#if DEBUG_MEM
        private void AboutMouseUp(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right) ShowDebugForm();
        }

        // Debug build only: memory figures for the running process, plus a forced cleanup.
        private void ShowDebugForm()
        {
            if (_debugFormOpen) return;
            _debugFormOpen = true;
            using (var owner = new Form
            {
                StartPosition = FormStartPosition.CenterScreen,
                ShowInTaskbar = false,
                TopMost = true,
                Opacity = 0,
                Size = new Size(0, 0)
            })
            using (var dlg = new Form
            {
                Text = "TrayHostMon - debug",
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                ShowInTaskbar = false,
                ClientSize = new Size(220, 80)
            })
            {
                var mem = new Button { Text = "Memory info", Location = new Point(12, 12), Width = 95, Height = 27 };
                var force = new Button { Text = "Force cleanup", Location = new Point(12, 45), Width = 95, Height = 27 };
                var close = new Button { Text = "Close", Location = new Point(133, 45), Width = 75, Height = 27, DialogResult = DialogResult.Cancel };
                mem.Click += (s, e) => MessageBox.Show(dlg, Diagnostics.Snapshot(), "TrayHostMon - memory", MessageBoxButtons.OK, MessageBoxIcon.Information);
                force.Click += (s, e) => MessageBox.Show(dlg, Diagnostics.ForceCleanup(), "TrayHostMon - memory", MessageBoxButtons.OK, MessageBoxIcon.Information);
                dlg.Controls.Add(mem);
                dlg.Controls.Add(force);
                dlg.Controls.Add(close);
                dlg.CancelButton = close;
                owner.Show();
                dlg.ShowDialog(owner);
            }
            _debugFormOpen = false;
        }
#endif

        // Removes a host from the running list, stops its monitor, and writes the config.
        private void RemoveHost(HostState st)
        {
            StopMonitor(st);
            _states.Remove(st);
            StateLog.Write(st.Cfg.Address, "REMOVED", null);
            SaveConfig();
            Refresh();
            if (_widget != null && !_widget.IsDisposed) _widget.Relayout();
        }

        // Toggles a host's enabled state, starts or stops monitoring, and writes the config.
        private void ToggleHostEnabled(HostState st)
        {
            st.Enabled = !st.Enabled;
            if (st.Enabled)
            {
                StartMonitor(st);
                StateLog.Write(st.Cfg.Address, "ENABLED", null);
            }
            else
            {
                StopMonitor(st);
                StateLog.Write(st.Cfg.Address, "DISABLED", null);
            }
            SaveConfig();
            Refresh();
            if (_widget != null && !_widget.IsDisposed) _widget.Invalidate();
        }

        private void ShowBalloon(string text)
        {
            _tray.BalloonTipTitle = "TrayHostMon";
            _tray.BalloonTipText = text;
            _tray.ShowBalloonTip(3000);
        }

        private void ToggleWidget()
        {
            if (_widget != null && !_widget.IsDisposed)
            {
                _widget.Close();
                return;
            }
            // Ignore the click that closed the widget (focus loss closes it first).
            if ((DateTime.Now - _widgetClosedAt).TotalMilliseconds < 300) return;

            _widget = new WidgetForm(_states, _paused);
            _widget.RemoveRequested += RemoveHost;
            _widget.ToggleEnabledRequested += ToggleHostEnabled;
            _widget.FormClosed += OnWidgetClosed;
            _widget.PlaceAt(_config.WidgetPos);
            _widget.Show();
            _widget.BringToFront(_widget.Handle);
        }

        // Remembers the widget position in memory when it was dragged. It is written on the next config save.
        private void OnWidgetClosed(object sender, FormClosedEventArgs e)
        {
            _widgetClosedAt = DateTime.Now;
            WidgetForm w = (WidgetForm)sender;
            if (w.Moved)
            {
                _config.WidgetPos = w.Location;
                SaveConfig();
            }
        }

        private void ToggleAutostart()
        {
            try
            {
                if (Autostart.IsEnabled) Autostart.Disable();
                else Autostart.Enable();
            }
            catch (Exception ex)
            {
                ShowBalloon("Autostart error: " + ex.Message);
            }
        }

        // Runs once, from the Exit item or Windows session end.
        private void ExitApp()
        {
            if (_exited) return;
            _exited = true;
            SystemEvents.SessionEnding -= OnSessionEnding;

            _lifetime.Cancel();
            StopMonitors();
            StateLog.Write("TrayHostMon", _paused ? "STOP (paused)" : "STOP", null);
            if (_widget != null && !_widget.IsDisposed) _widget.Close();
            _tray.Visible = false;
            ExitThread();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                SystemEvents.SessionEnding -= OnSessionEnding;
                _lifetime.Dispose();
                _tray.Dispose();
                _menu.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
