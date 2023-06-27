using System.Drawing;
using System.Drawing.Imaging;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;

namespace MemoryMappedFiles
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "<Pending>")]
    public class MemFileIO
    {
        public static async Task<Bitmap> ReadBitmapFromMemoryMappedFile(string memFilePath, Size size)
        {
            var bitmap = new Bitmap(size.Width, size.Height, PixelFormat.Format32bppRgb);

            try
            {
                var memFile = MemoryMappedFile.OpenExisting(memFilePath);
                var bmpData = bitmap.LockBits(
                    new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                    ImageLockMode.ReadWrite,
                    PixelFormat.Format32bppRgb);

                using MemoryMappedViewStream stream = memFile.CreateViewStream();
                var length = bitmap.Width * bitmap.Height * 4;
                byte[] buffer = new byte[length];
                _ = await stream.ReadAsync(buffer.AsMemory(0, length), default);
                Marshal.Copy(buffer, 0, bmpData.Scan0, length);

                bitmap.UnlockBits(bmpData);
            }
            catch (FileNotFoundException)
            {
                Console.WriteLine("Couldn't find memory mapped video file.");
            }
            return bitmap;
        }

        public async static Task<MemoryMappedFile> WriteBitmapToMemoryMappedFile(Bitmap bitmap, string memFilePath)
        {
            var memFile = MemoryMappedFile.CreateOrOpen(memFilePath, bitmap.Width * bitmap.Height * 4);
            using MemoryMappedViewStream stream = memFile.CreateViewStream();

            var bmpData = bitmap.LockBits(
                new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                ImageLockMode.ReadOnly,
                PixelFormat.Format32bppRgb);

            var length = bitmap.Width * bitmap.Height * 4;
            byte[] buffer = new byte[length];
            Marshal.Copy(bmpData.Scan0, buffer, 0, length);
            await stream.WriteAsync(buffer.AsMemory(0, length), default);
            bitmap.UnlockBits(bmpData);
            return memFile;
        }
    }
}