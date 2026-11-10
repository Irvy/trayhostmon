using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace TrayHostMon
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

            Text = "TrayHostMon - add host / settings";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            TopMost = true;
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(380, 118);
            Font = new Font("Segoe UI", 9f);

            var label = new Label
            {
                Text = "Add hosts (separate with , ; or space)",
                Location = new Point(12, 8),
                AutoSize = true
            };
            _input = new TextBox { Location = new Point(12, 32), Width = 356 };

            // Three compact inputs in a row with labels beneath; OK aligns with the inputs on the right.
            _intervalInput = new TextBox { Location = new Point(12, 66), Width = 60, Text = currentInterval.ToString() };
            _timeoutInput = new TextBox { Location = new Point(88, 66), Width = 60, Text = currentTimeout.ToString() };
            _bufferInput = new TextBox { Location = new Point(164, 66), Width = 60, Text = currentBuffer.ToString() };

            var periodLabel = new Label { Text = "period", Location = new Point(12, 90), AutoSize = true };
            var timeoutLabel = new Label { Text = "timeout", Location = new Point(88, 90), AutoSize = true };
            var bufferLabel = new Label { Text = "size", Location = new Point(164, 90), AutoSize = true };

            _message = new Label { Location = new Point(12, 118), Size = new Size(356, 16), ForeColor = Color.FromArgb(218, 54, 51) };
            var ok = new Button { Text = "OK", Location = new Point(293, 66), Size = new Size(75, 23) };

            ok.Click += (s, e) => Accept();
            AcceptButton = ok;
            // No Cancel button or CancelButton: the window close button is the only way to cancel.

            Controls.Add(label);
            Controls.Add(_input);
            Controls.Add(_intervalInput);
            Controls.Add(_timeoutInput);
            Controls.Add(_bufferInput);
            Controls.Add(periodLabel);
            Controls.Add(timeoutLabel);
            Controls.Add(bufferLabel);
            Controls.Add(_message);
            Controls.Add(ok);

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

