using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace AcsWindowsClient
{
    internal static class BitmapExtensions
    {
        public static bool IsSolidColor(this Bitmap bitmap, Color color)
        {
            var bmpData = bitmap.LockBits(
                new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                ImageLockMode.ReadOnly,
                PixelFormat.Format32bppRgb);

            var intColor = color.ToArgb();
            var stride = bitmap.Width * 4;

            for (int x = 0; x < bitmap.Height; x += 1)
            {
                var offset = x * stride;
                for (int y = 0; y < bitmap.Width; y += 4)
                {
                    var data = Marshal.ReadInt32(bmpData.Scan0 + offset + y);
                    if (data != intColor)
                    {
                        bitmap.UnlockBits(bmpData);
                        return false;
                    }
                }
            }
            bitmap.UnlockBits(bmpData);
            return true;
        }

        public static bool TestFirstPixel(this Bitmap bitmap)
        {
            var bmpData = bitmap.LockBits(
                new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                ImageLockMode.ReadOnly,
                PixelFormat.Format32bppRgb);

            var data = Marshal.ReadInt32(bmpData.Scan0);
            bitmap.UnlockBits(bmpData);
            return data == 0;
        }
    }
}
