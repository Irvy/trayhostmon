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
        private readonly Label _message;

        // New hosts accepted by OK (normalized, no duplicates).
        public List<string> Hosts { get; private set; }
        // Entries that were valid but already in the running list.
        public List<string> AlreadyListed { get; private set; }

        public AddHostForm(Func<string, bool> isListed)
        {
            _isListed = isListed;
            Hosts = new List<string>();
            AlreadyListed = new List<string>();

            Text = "pingmon - add host";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            TopMost = true;
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(380, 110);
            Font = new Font("Segoe UI", 9f);

            var label = new Label
            {
                Text = "IP addresses or hostnames (space, comma or semicolon separated):",
                Location = new Point(12, 12),
                AutoSize = true
            };
            _input = new TextBox { Location = new Point(12, 34), Width = 356 };
            _message = new Label { Location = new Point(12, 60), Size = new Size(356, 16), ForeColor = Color.FromArgb(218, 54, 51) };
            var ok = new Button { Text = "OK", Location = new Point(212, 80), Size = new Size(75, 23) };
            var cancel = new Button { Text = "Cancel", Location = new Point(293, 80), Size = new Size(75, 23) };

            ok.Click += (s, e) => Accept();
            cancel.Click += (s, e) => Close();
            AcceptButton = ok;
            CancelButton = cancel;

            Controls.Add(label);
            Controls.Add(_input);
            Controls.Add(_message);
            Controls.Add(ok);
            Controls.Add(cancel);

            Shown += (s, e) => _input.Focus();
        }

        // Entries may be separated by spaces, commas, semicolons or tabs.
        private void Accept()
        {
            string[] entries = _input.Text.Split(new[] { ' ', ',', ';', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (entries.Length == 0)
            {
                _message.Text = "enter at least one IP or hostname";
                return;
            }

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
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}

