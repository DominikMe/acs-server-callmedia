using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Windows.Foundation;
using WinRT;

namespace AcsWindowsClient
{
    internal static class Conversions
    {
        [ComImport]
        [Guid("5B0D3235-4DBA-4D44-865E-8F1D0E4FD04D")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        unsafe interface IMemoryBufferByteAccess
        {
            void GetBuffer(out byte* buffer, out uint capacity);
        }

        public static unsafe MemoryBuffer ToMemoryBuffer(this Bitmap bitmap)
        {
            var bmpData = bitmap.LockBits(
                    new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                    ImageLockMode.ReadOnly,
                    PixelFormat.Format32bppArgb);

            int length = bmpData.Stride * bmpData.Height;
            var memoryBuffer = new MemoryBuffer((uint)length);
            using (IMemoryBufferReference reference = memoryBuffer.CreateReference())
            {
                reference.As<IMemoryBufferByteAccess>().GetBuffer(out byte* dataInBytes, out uint capacityInBytes);
                IntPtr destination = new(dataInBytes);
                WriteBitmap(bmpData, destination);
            }
            bitmap.UnlockBits(bmpData);
            return memoryBuffer;
        }

        private static void WriteBitmap(this BitmapData bmpData, IntPtr destination)
        {
            for (var row = 0; row < bmpData.Height; row++)
            {
                var rowStart = row * bmpData.Stride;
                for (var col = 0; col < bmpData.Stride; col += 4)
                {
                    var value = Marshal.ReadInt32(bmpData.Scan0, rowStart + col);
                    Marshal.WriteInt32(destination, rowStart + col, ArgbToRgba((uint)value));
                }
            }
        }

        // actually needs to be abgr to match what ACS calling expects for Rgba
        private static int ArgbToRgba(uint argb)
            => (int)(
                (argb & 0xff00ff00) |
                ((argb & 0x00ff0000) >> 16) |
                ((argb & 0x000000ff) << 16));
    }
}
