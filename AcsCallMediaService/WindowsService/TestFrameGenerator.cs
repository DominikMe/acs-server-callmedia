using System.Drawing.Imaging;
using System.Drawing;
using System.Runtime.InteropServices;

namespace WindowsService
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "<Pending>")]
    public class TestFrameGenerator
    {
        public static Bitmap CreateFrame(Size size, int position, int width)
        {
            var bitmap = new Bitmap(size.Width, size.Height);
            var bmpData = bitmap.LockBits(
                new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                ImageLockMode.ReadWrite,
                PixelFormat.Format32bppArgb);

            CreateFrame(bmpData.Scan0, size, position, width);

            bitmap.UnlockBits(bmpData);
            return bitmap;
        }

        private static int foreground = Color.White.ToArgb();
        private static int background = Color.Blue.ToArgb();

        private static void CreateFrame(IntPtr destination, Size size, int position, int width)
        {
            var barStart = position * 4;
            var barEnd = (position + width) * 4;
            for (var row = 0; row < size.Height; row++)
            {
                var rowStart = row * size.Width * 4;
                for (var col = 0; col < Math.Min(barStart, size.Width * 4); col += 4)
                {
                    Marshal.WriteInt32(destination, rowStart + col, background);
                }
                for (var col = barStart; col < Math.Min(barEnd, size.Width * 4); col += 4)
                {
                    Marshal.WriteInt32(destination, rowStart + col, foreground);
                }
                for (var col = barEnd; col < size.Width * 4; col += 4)
                {
                    Marshal.WriteInt32(destination, rowStart + col, background);
                }
            }
        }
    }
}
