using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace PingMon
{
    public static class IconFactory
    {
        private static readonly Dictionary<Overall, Icon> Cache = new Dictionary<Overall, Icon>();

        public static Icon Get(Overall overall)
        {
            Icon icon;
            if (Cache.TryGetValue(overall, out icon)) return icon;

            Color color;
            switch (overall)
            {
                case Overall.AllUp: color = Color.FromArgb(46, 160, 67); break;
                case Overall.DownUnacked: color = Color.FromArgb(218, 54, 51); break;
                case Overall.DownAcked: color = Color.FromArgb(227, 140, 26); break;
                default: color = Color.FromArgb(140, 140, 140); break; // Paused
            }

            icon = Make(color);
            Cache[overall] = icon;
            return icon;
        }

        private static Icon Make(Color color)
        {
            using (var bmp = new Bitmap(16, 16))
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);
                using (var fill = new SolidBrush(color))
                using (var outline = new Pen(Color.FromArgb(60, 60, 60), 1))
                {
                    g.FillEllipse(fill, 1, 1, 13, 13);
                    g.DrawEllipse(outline, 1, 1, 13, 13);
                }
                IntPtr hIcon = bmp.GetHicon();
                // Copy so the GDI handle can be released.
                Icon copy = (Icon)Icon.FromHandle(hIcon).Clone();
                NativeMethods.DestroyIcon(hIcon);
                return copy;
            }
        }
    }
}
