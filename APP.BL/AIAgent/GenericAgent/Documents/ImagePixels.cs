using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

namespace App.BL.AIAgent.GenericAgent.Documents
{
    /// <summary>Pixel size of a jpeg, png, gif, or bmp. Returns 0,0 when the header is not recognized.</summary>
    public static class ImagePixels
    {
        public static (int Width, int Height) Read(byte[] bytes)
        {
            if (bytes == null || bytes.Length < 8) return (0, 0);
            if (bytes[0] == 0x89 && bytes[1] == 0x50 && bytes.Length >= 24)
                return (Be32(bytes, 16), Be32(bytes, 20));
            if (bytes[0] == (byte)'G' && bytes[1] == (byte)'I' && bytes.Length >= 10)
                return (Le16(bytes, 6), Le16(bytes, 8));
            if (bytes[0] == (byte)'B' && bytes[1] == (byte)'M' && bytes.Length >= 26)
                return (Le32(bytes, 18), Math.Abs(Le32(bytes, 22)));
            if (bytes[0] == 0xFF && bytes[1] == 0xD8)
                return Jpeg(bytes);
            return (0, 0);
        }

        /// <summary>Largest box that keeps the picture's aspect ratio inside maxWidth and maxHeight.</summary>
        public static (int Width, int Height) Fit(byte[] bytes, int maxWidth, int maxHeight)
        {
            if (maxWidth < 1) maxWidth = 1;
            if (maxHeight < 1) maxHeight = 1;
            var size = Read(bytes);
            if (size.Width <= 0 || size.Height <= 0)
                return (maxWidth, maxHeight);
            var scale = Math.Min(maxWidth / (double)size.Width, maxHeight / (double)size.Height);
            return (Math.Max(1, (int)Math.Round(size.Width * scale)), Math.Max(1, (int)Math.Round(size.Height * scale)));
        }

        /// <summary>
        /// Draw the image centered in a boxW×boxH canvas (letterbox / pillarbox) as PNG.
        /// Use before PPT/Word cell picture fills that stretch-to-fill — the padded bitmap
        /// keeps the visual aspect ratio when the fill stretches to the cell.
        /// </summary>
        public static byte[] ContainPng(byte[] bytes, int boxW, int boxH)
        {
            if (bytes == null || bytes.Length == 0) return bytes;
            if (boxW < 1) boxW = 1;
            if (boxH < 1) boxH = 1;
            try
            {
                using var input = new MemoryStream(bytes);
                using var src = Image.FromStream(input, useEmbeddedColorManagement: false, validateImageData: false);
                var fit = Fit(bytes, boxW, boxH);
                if (fit.Width <= 0 || fit.Height <= 0)
                    fit = (boxW, boxH);
                using var canvas = new Bitmap(boxW, boxH);
                using (var g = Graphics.FromImage(canvas))
                {
                    g.Clear(Color.White);
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    g.SmoothingMode = SmoothingMode.HighQuality;
                    var dx = Math.Max(0, (boxW - fit.Width) / 2);
                    var dy = Math.Max(0, (boxH - fit.Height) / 2);
                    g.DrawImage(src, new Rectangle(dx, dy, fit.Width, fit.Height));
                }
                using var output = new MemoryStream();
                canvas.Save(output, ImageFormat.Png);
                return output.ToArray();
            }
            catch
            {
                return bytes;
            }
        }

        private static (int Width, int Height) Jpeg(byte[] bytes)
        {
            var i = 2;
            while (i + 8 < bytes.Length)
            {
                if (bytes[i] != 0xFF)
                {
                    i++;
                    continue;
                }
                var marker = bytes[i + 1];
                if (marker == 0xC0 || marker == 0xC1 || marker == 0xC2)
                {
                    if (i + 9 >= bytes.Length) return (0, 0);
                    return (Be16(bytes, i + 7), Be16(bytes, i + 5));
                }
                if (marker == 0xD8 || marker == 0xD9 || (marker >= 0xD0 && marker <= 0xD7))
                {
                    i += 2;
                    continue;
                }
                if (i + 3 >= bytes.Length) return (0, 0);
                var len = Be16(bytes, i + 2);
                if (len < 2) return (0, 0);
                i += 2 + len;
            }
            return (0, 0);
        }

        private static int Be16(byte[] bytes, int offset)
            => (bytes[offset] << 8) | bytes[offset + 1];

        private static int Be32(byte[] bytes, int offset)
            => (bytes[offset] << 24) | (bytes[offset + 1] << 16) | (bytes[offset + 2] << 8) | bytes[offset + 3];

        private static int Le16(byte[] bytes, int offset)
            => bytes[offset] | (bytes[offset + 1] << 8);

        private static int Le32(byte[] bytes, int offset)
            => bytes[offset] | (bytes[offset + 1] << 8) | (bytes[offset + 2] << 16) | (bytes[offset + 3] << 24);
    }
}
