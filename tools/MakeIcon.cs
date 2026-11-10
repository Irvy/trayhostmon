using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

// Build-time helper: renders the TrayHostMon tray-state circle into an .ico file,
// using the same drawing as TrayHostMon.IconFactory.Make (transparent background,
// anti-aliased filled circle + rgb(60,60,60) outline). At size 16 the geometry
// is identical to IconFactory (margin 1, diameter 13, pen 1).
//
// The ICO is written by hand as 32-bpp BGRA DIB images + a 1-bpp AND mask so
// colour and transparency survive. (System.Drawing.Icon.Save would re-encode
// to a 16-colour 4-bpp DIB and destroy both.)
//
// Kept in tools\ on purpose so build.cmd's `*.cs` glob does not compile it into
// the app (which would cause a duplicate Main). Standalone: no project deps.
//
//   makeicon.exe [state] [out.ico] [sizes]
//     state   allup (default) | downunacked | downacked | paused
//     out.ico target file, default "TrayHostMon.ico" (relative to the cwd)
//     sizes   comma list, default "16,32,48"
static class MakeIcon
{
    private static readonly int[] DefaultSizes = { 16, 32, 48 };

    private static int Main(string[] args)
    {
        string state = args.Length > 0 ? args[0].ToLowerInvariant() : "allup";
        string outPath = args.Length > 1 ? args[1] : "TrayHostMon.ico";

        int[] sizes;
        try
        {
            sizes = ParseSizes(args.Length > 2 ? args[2] : null);
        }
        catch (ArgumentException ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 2;
        }

        // Same palette as TrayHostMon.IconFactory.Get / TrayHostMon.Overall.
        Color color;
        switch (state)
        {
            case "allup": color = Color.FromArgb(46, 160, 67); break;       // green
            case "downunacked": color = Color.FromArgb(218, 54, 51); break; // red
            case "downacked": color = Color.FromArgb(227, 140, 26); break;  // orange
            case "paused": color = Color.FromArgb(140, 140, 140); break;    // grey
            default:
                Console.Error.WriteLine("Unknown state '" + state +
                    "'. Use: allup | downunacked | downacked | paused");
                return 2;
        }

        var dibs = new List<byte[]>();
        foreach (int s in sizes)
        {
            using (var bmp = DrawCircle(color, s))
            {
                dibs.Add(BuildDib(bmp));
            }
        }

        WriteIco(outPath, sizes, dibs);
        Console.WriteLine("Wrote " + outPath + " (" + state + ", sizes " +
            string.Join(",", Array.ConvertAll(sizes, delegate(int v) { return v.ToString(); })) + ")");
        return 0;
    }

    // Mirrors IconFactory.Make, scaled: margin = size/16, diameter = 13*size/16,
    // outline = max(1, size/16). At size 16 this is exactly (1,1,13,13)/pen 1.
    private static Bitmap DrawCircle(Color color, int size)
    {
        var bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);
            float margin = size / 16f;
            float diameter = 13f * size / 16f;
            float pen = Math.Max(1f, size / 16f);
            using (var fill = new SolidBrush(color))
            using (var outline = new Pen(Color.FromArgb(60, 60, 60), pen))
            {
                g.FillEllipse(fill, margin, margin, diameter, diameter);
                g.DrawEllipse(outline, margin, margin, diameter, diameter);
            }
        }
        return bmp;
    }

    // Returns one icon image in BMP (DIB) form: BITMAPINFOHEADER (biHeight=2*h),
    // XOR 32-bpp BGRA bottom-up, then the 1-bpp AND mask.
    private static byte[] BuildDib(Bitmap bmp)
    {
        int w = bmp.Width;
        int h = bmp.Height;

        BitmapData data = bmp.LockBits(new Rectangle(0, 0, w, h),
            ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
        int stride = data.Stride;
        byte[] pixels = new byte[stride * h];
        Marshal.Copy(data.Scan0, pixels, 0, pixels.Length);
        bmp.UnlockBits(data);

        int maskStride = ((w + 31) / 32) * 4;
        byte[] mask = new byte[maskStride * h];

        using (var ms = new MemoryStream())
        using (var bw = new BinaryWriter(ms))
        {
            bw.Write(40);        // biSize
            bw.Write(w);         // biWidth
            bw.Write(h * 2);     // biHeight (XOR + AND)
            bw.Write((short)1);  // biPlanes
            bw.Write((short)32); // biBitCount
            bw.Write(0);         // biCompression (BI_RGB)
            bw.Write(w * h * 4 + mask.Length); // biSizeImage
            bw.Write(0);         // biXPelsPerMeter
            bw.Write(0);         // biYPelsPerMeter
            bw.Write(0);         // biClrUsed
            bw.Write(0);         // biClrImportant

            // XOR bitmap, bottom-up (ICON DIBs are stored bottom-up).
            for (int dstRow = 0; dstRow < h; dstRow++)
            {
                int srcRow = h - 1 - dstRow;
                bw.Write(pixels, srcRow * stride, w * 4);

                // AND mask: bit = 1 marks a transparent pixel.
                int maskBase = dstRow * maskStride;
                for (int x = 0; x < w; x++)
                {
                    byte a = pixels[srcRow * stride + x * 4 + 3];
                    if (a < 128) mask[maskBase + (x >> 3)] |= (byte)(0x80 >> (x & 7));
                }
            }

            bw.Write(mask);      // AND mask, bottom-up
            bw.Flush();
            return ms.ToArray();
        }
    }

    private static void WriteIco(string path, int[] sizes, List<byte[]> dibs)
    {
        using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write))
        using (var bw = new BinaryWriter(fs))
        {
            bw.Write((short)0);          // reserved
            bw.Write((short)1);          // type = icon
            bw.Write((short)sizes.Length);

            int offset = 6 + 16 * sizes.Length;
            for (int i = 0; i < sizes.Length; i++)
            {
                int s = sizes[i];
                bw.Write((byte)(s >= 256 ? 0 : s)); // width (0 means 256)
                bw.Write((byte)(s >= 256 ? 0 : s)); // height
                bw.Write((byte)0);   // colour count
                bw.Write((byte)0);   // reserved
                bw.Write((short)1);  // planes
                bw.Write((short)32); // bit count
                bw.Write(dibs[i].Length);
                bw.Write(offset);
                offset += dibs[i].Length;
            }

            foreach (byte[] dib in dibs) bw.Write(dib);
            bw.Flush();
        }
    }

    private static int[] ParseSizes(string spec)
    {
        if (string.IsNullOrEmpty(spec)) return DefaultSizes;
        string[] parts = spec.Split(',');
        var list = new List<int>();
        foreach (string raw in parts)
        {
            int v;
            if (!int.TryParse(raw.Trim(), out v) || v < 1 || v > 256)
                throw new ArgumentException("Invalid size '" + raw + "' (use 1..256)");
            list.Add(v);
        }
        if (list.Count == 0) return DefaultSizes;
        return list.ToArray();
    }
}
