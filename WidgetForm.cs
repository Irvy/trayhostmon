using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace PingMon
{
    public class WidgetForm : Form
    {
        private const int RowHeight = 20;
        private const int Pad = 8;
        private const int HeaderHeight = 18;
        private const int WM_NCHITTEST = 0x0084;
        private const int HTCLIENT = 1;
        private const int HTCAPTION = 2;
        private const int RemoveWidth = 14;
        private const int WidthBase = 360;
        private const int RowIndent = 18; // space taken by the remove button at the row start

        private readonly List<HostState> _states;
        private readonly Timer _timer;
        private readonly bool _paused;
        private readonly Timer _outsideTimer;
        private int _hoverRow = -1;

        // Set when the user drags the window; the caller saves the position on close.
        public bool Moved { get; private set; }

        // Raised when the user clicks the remove button of a row.
        public event Action<HostState> RemoveRequested;

        public WidgetForm(List<HostState> states, bool paused)
        {
            _states = states;
            _paused = paused;

            FormBorderStyle = FormBorderStyle.None;
            TopMost = true;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            MaximizeBox = false;
            MinimizeBox = false;
            ControlBox = false;
            BackColor = Color.FromArgb(32, 32, 32);
            DoubleBuffered = true;
            KeyPreview = true;

            _timer = new Timer { Interval = 1000 };
            _timer.Tick += (s, e) => Invalidate();
            _timer.Start();

            // Fallback for outside clicks when focus-loss doesn't fire.
            _outsideTimer = new Timer { Interval = 50 };
            _outsideTimer.Tick += (s, e) => CheckOutsideClick();
            _outsideTimer.Start();

            Deactivate += (s, e) => Close();
            KeyDown += (s, e) => { if (e.KeyCode == Keys.Escape) Close(); };
            MouseMove += (s, e) => UpdateHover(e.Location);
            MouseLeave += (s, e) => { _hoverRow = -1; Invalidate(); };
            MouseDown += (s, e) => OnRowClick(e);

            Size = MeasureSize();
        }

        // Brings the window to the front so clicks outside it are detected.
        public void BringToFront(IntPtr hwnd)
        {
            NativeMethods.SetForegroundWindow(hwnd);
        }

        private void CheckOutsideClick()
        {
            if (!Visible || IsDisposed) return;
            bool down = (NativeMethods.GetAsyncKeyState(0x01) & 0x8000) != 0
                     || (NativeMethods.GetAsyncKeyState(0x02) & 0x8000) != 0;
            if (!down) return;
            if (!Bounds.Contains(Cursor.Position)) Close();
        }

        private int RowAt(Point p)
        {
            int top = Pad + (_paused ? HeaderHeight : 0);
            if (p.Y < top) return -1;
            int row = (p.Y - top) / RowHeight;
            return row < _states.Count ? row : -1;
        }

        // The remove button sits at the left edge of each row.
        private bool OnRemoveButton(Point p)
        {
            return p.X >= Pad && p.X <= Pad + RemoveWidth;
        }

        private void UpdateHover(Point p)
        {
            int row = RowAt(p);
            int hover = (row >= 0 && OnRemoveButton(p)) ? row : -1;
            if (hover != _hoverRow)
            {
                _hoverRow = hover;
                Invalidate();
            }
        }

        private void OnRowClick(MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            int row = RowAt(e.Location);
            if (row < 0 || !OnRemoveButton(e.Location)) return;
            var handler = RemoveRequested;
            if (handler != null) handler(_states[row]);
        }

        // Recalculates the size after the host list changed, keeping the top-left corner.
        public void Relayout()
        {
            Size = MeasureSize();
            Invalidate();
        }

        // Makes the whole window draggable with the left button (acts as a title bar).
        // The remove-button column stays clickable; the rest of the window acts as a title bar.
        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
            if (m.Msg == WM_NCHITTEST && m.Result == (IntPtr)HTCLIENT)
            {
                Point p = PointToClient(new Point((int)m.LParam & 0xFFFF, ((int)m.LParam >> 16) & 0xFFFF));
                if (!(RowAt(p) >= 0 && OnRemoveButton(p)))
                    m.Result = (IntPtr)HTCAPTION;
            }
        }

        protected override void OnMove(EventArgs e)
        {
            base.OnMove(e);
            if (Visible) Moved = true;
        }

        // Places at the saved top-left if it's still on a screen, otherwise at the cursor.
        public void PlaceAt(Point? saved)
        {
            if (saved.HasValue)
            {
                Rectangle rect = new Rectangle(saved.Value, Size);
                bool onAnyScreen = false;
                foreach (var s in Screen.AllScreens)
                    if (s.WorkingArea.IntersectsWith(rect)) onAnyScreen = true;

                if (onAnyScreen)
                {
                    Rectangle area = Screen.FromRectangle(rect).WorkingArea;
                    int x = Math.Max(area.Left, Math.Min(rect.Left, area.Right - Width));
                    int y = Math.Max(area.Top, Math.Min(rect.Top, area.Bottom - Height));
                    Location = new Point(x, y);
                    return;
                }
            }

            Point p = Cursor.Position;
            Rectangle work = Screen.FromPoint(p).WorkingArea;
            int cx = p.X - Width;
            int cy = p.Y - Height;
            if (cx < work.Left) cx = work.Left;
            if (cy < work.Top) cy = work.Top;
            if (cx + Width > work.Right) cx = work.Right - Width;
            if (cy + Height > work.Bottom) cy = work.Bottom - Height;
            Location = new Point(cx, cy);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            using (var font = new Font("Segoe UI", 9f))
            using (var white = new SolidBrush(Color.White))
            using (var grey = new SolidBrush(Color.Silver))
            using (var dim = new SolidBrush(Color.FromArgb(110, 110, 110)))
            using (var red = new SolidBrush(Color.FromArgb(218, 54, 51)))
            using (var border = new Pen(Color.FromArgb(70, 70, 70)))
            {
                int y = Pad;
                if (_paused)
                {
                    g.DrawString("paused", font, grey, Pad, y);
                    y += HeaderHeight;
                }

                if (_states.Count == 0)
                {
                    g.DrawString("no hosts - use 'Add host' in the tray menu", font, grey, Pad, y);
                }

                int row = 0;
                foreach (var h in _states)
                {
                    // Tiny remove button at the row start: grey normally, red while hovered.
                    Brush xBrush = (row == _hoverRow) ? red : dim;
                    g.DrawString("\u00d7", font, xBrush, Pad, y - 2);

                    Color dotColor = _paused ? Color.FromArgb(90, 90, 90) : DotColor(h);
                    using (var dot = new SolidBrush(dotColor))
                        g.FillEllipse(dot, Pad + RowIndent, y + 5, 10, 10);

                    Brush text = _paused ? dim : white;
                    Brush sub = _paused ? dim : grey;
                    g.DrawString(h.Cfg.Address, font, text, Pad + RowIndent + 16, y);
                    string rtt = h.LastRttMs < 0 ? "\u2014" : h.LastRttMs + " ms";
                    g.DrawString(rtt, font, sub, Pad + RowIndent + 160, y);
                    g.DrawString(StatusText(h, _paused), font, sub, Pad + RowIndent + 230, y);

                    y += RowHeight;
                    row++;
                }
                g.DrawRectangle(border, 0, 0, Width - 1, Height - 1);
            }
        }

        private Size MeasureSize()
        {
            int width = WidthBase + RowIndent + 4;
            int rows = Math.Max(_states.Count, 1);
            int height = Pad * 2 + RowHeight * rows + (_paused ? HeaderHeight : 0);
            return new Size(width, height);
        }

        private static Color DotColor(HostState h)
        {
            switch (h.State)
            {
                case State.Up: return Color.FromArgb(46, 160, 67);
                case State.Down: return h.Acked ? Color.FromArgb(227, 140, 26) : Color.FromArgb(218, 54, 51);
                default: return Color.FromArgb(140, 140, 140);
            }
        }

        private static string StatusText(HostState h, bool paused)
        {
            if (!paused) return StateWord(h);
            string asOf = h.LastCheck == DateTime.MinValue ? "\u2014" : h.LastCheck.ToString("HH:mm:ss");
            return StateWord(h) + " \u00b7 as of " + asOf;
        }

        private static string StateWord(HostState h)
        {
            if (h.State == State.Up) return "up";
            if (h.State == State.Unknown) return "unknown";

            DateTime since = h.Since;
            string when = since.Date == DateTime.Today
                ? since.ToString("HH:mm")
                : since.ToString("dd.MM HH:mm");
            return "down since " + when + (h.Acked ? " (ack)" : "");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _timer.Dispose();
                _outsideTimer.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
