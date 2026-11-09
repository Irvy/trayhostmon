using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace PingMon
{
    public class AddHostForm : Form
    {
        private readonly Func<string, bool> _isListed;
        private readonly TextBox _input;
        private readonly TextBox _intervalInput;
        private readonly TextBox _timeoutInput;
        private readonly TextBox _bufferInput;
        private readonly Label _message;

        // New hosts accepted by OK (normalized, no duplicates).
        public List<string> Hosts { get; private set; }
        // Entries that were valid but already in the running list.
        public List<string> AlreadyListed { get; private set; }
        // Settings changes (null = unchanged)
        public int? IntervalSec { get; private set; }
        public int? TimeoutMs { get; private set; }
        public int? Buffer { get; private set; }

        public AddHostForm(Func<string, bool> isListed, int currentInterval = 5, int currentTimeout = 1000, int currentBuffer = 32)
        {
            _isListed = isListed;
            Hosts = new List<string>();
            AlreadyListed = new List<string>();

            Text = "pingmon - add host / settings";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            TopMost = true;
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(380, 250);
            Font = new Font("Segoe UI", 9f);

            int y = 12;

            var label = new Label
            {
                Text = "IP addresses or hostnames (space, comma or semicolon separated):",
                Location = new Point(12, y)
            };
            y += 22;
            _input = new TextBox { Location = new Point(12, y), Width = 356 };
            y += 34;

            // Settings row 1: Interval
            var intervalLabel = new Label
            {
                Text = "Ping interval (seconds):",
                Location = new Point(12, y),
                AutoSize = true
            };
            _intervalInput = new TextBox
            {
                Location = new Point(160, y - 2),
                Width = 60,
                Text = currentInterval.ToString()
            };
            y += 28;

            // Settings row 2: Timeout
            var timeoutLabel = new Label
            {
                Text = "Timeout (ms):",
                Location = new Point(12, y),
                AutoSize = true
            };
            _timeoutInput = new TextBox
            {
                Location = new Point(160, y - 2),
                Width = 60,
                Text = currentTimeout.ToString()
            };
            y += 28;

            // Settings row 3: Buffer
            var bufferLabel = new Label
            {
                Text = "Payload size (bytes, 0=default):",
                Location = new Point(12, y),
                AutoSize = true
            };
            _bufferInput = new TextBox
            {
                Location = new Point(230, y - 2),
                Width = 60,
                Text = currentBuffer.ToString()
            };
            y += 28;

            _message = new Label { Location = new Point(12, y), Size = new Size(356, 16), ForeColor = Color.FromArgb(218, 54, 51) };
            y += 24;

            var ok = new Button { Text = "OK", Location = new Point(212, y), Size = new Size(75, 23) };
            var cancel = new Button { Text = "Cancel", Location = new Point(293, y), Size = new Size(75, 23) };

            ok.Click += (s, e) => Accept();
            cancel.Click += (s, e) => Close();
            AcceptButton = ok;
            CancelButton = cancel;

            Controls.Add(label);
            Controls.Add(_input);
            Controls.Add(intervalLabel);
            Controls.Add(_intervalInput);
            Controls.Add(timeoutLabel);
            Controls.Add(_timeoutInput);
            Controls.Add(bufferLabel);
            Controls.Add(_bufferInput);
            Controls.Add(_message);
            Controls.Add(ok);
            Controls.Add(cancel);

            Shown += (s, e) => _input.Focus();
        }

        // Entries may be separated by spaces, commas, semicolons or tabs.
        private void Accept()
        {
            string[] entries = _input.Text.Split(new[] { ' ', ',', ';', '\t' }, StringSplitOptions.RemoveEmptyEntries);

            // Validate settings
            int intervalVal, timeoutVal, bufferVal;
            if (!int.TryParse(_intervalInput.Text, out intervalVal) || intervalVal < 1 || intervalVal > 3600)
            {
                _message.Text = "interval must be 1-3600 seconds";
                return;
            }
            if (!int.TryParse(_timeoutInput.Text, out timeoutVal) || timeoutVal < 500 || timeoutVal > 10000)
            {
                _message.Text = "timeout must be 500-10000 ms";
                return;
            }
            if (!int.TryParse(_bufferInput.Text, out bufferVal) || bufferVal < 0 || bufferVal > 65500)
            {
                _message.Text = "buffer must be 0-65500 bytes";
                return;
            }

            // If no hosts entered, still allow settings-only change
            if (entries.Length == 0)
            {
                _message.Text = "only settings will be applied";
            }
            else
            {
                var invalid = new List<string>();
                var fresh = new List<string>();
                var listed = new List<string>();
                var seen = new HashSet<string>();

                foreach (string entry in entries)
                {
                    string host;
                    if (!Config.TryNormalizeHost(entry, out host))
                    {
                        invalid.Add(entry);
                        continue;
                    }
                    if (!seen.Add(host)) continue;

                    if (_isListed(host)) listed.Add(host);
                    else fresh.Add(host);
                }

                if (invalid.Count > 0)
                {
                    _message.Text = "invalid: " + string.Join(", ", invalid.ToArray());
                    return;
                }
                if (fresh.Count == 0)
                {
                    _message.Text = "already in list: " + string.Join(", ", listed.ToArray());
                    return;
                }

                Hosts = fresh;
                AlreadyListed = listed;
            }

            // Store settings values
            IntervalSec = intervalVal;
            TimeoutMs = timeoutVal;
            Buffer = bufferVal;

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}

