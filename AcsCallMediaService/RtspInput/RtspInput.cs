using OpenCvSharp;
using OpenCvSharp.Extensions;
using System.Drawing;
using Size = System.Drawing.Size;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace RtspInput
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "<Pending>")]
    public class RtspInput
    {
        private readonly string url;
        private readonly Size size;
        private readonly int fps;
        private readonly Func<Bitmap, ValueTask> onVideoFrame;
        private VideoCapture capture;
        private volatile bool isRunning;

        public RtspInput(string url, Size size, int fps, Func<Bitmap, ValueTask> onVideoFrame)
        {
            this.url = url;
            this.size = size;
            this.fps = fps;
            this.onVideoFrame = onVideoFrame;
        }

        public Task Start()
        {
            if (isRunning) return Task.CompletedTask;
            isRunning = true;

            capture = new VideoCapture(url);
            return Task.Run(async () =>
            {
                using Mat mat = new();
                while (isRunning)
                {
                    capture.Read(mat);
                    if (mat.Empty())
                        break;
                    Bitmap converted = mat.ToBitmap();
                    Bitmap resized = new(converted, size);
                    //window.ShowImage(converted.ToMat());
                    await onVideoFrame(resized);
                    Cv2.WaitKey(1000 / fps);
                }
            });
        }

        private static Bitmap ToBitmap(Mat mat)
        {
            using var ms = mat.ToMemoryStream();
            return (Bitmap)Image.FromStream(ms);
        }

        //[System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "<Pending>")]
        //public Bitmap? GrabFrame()
        //{
        //    if (capture?.IsDisposed ?? true) return null;
        //    using Mat mat = new();
        //    capture.Read(mat);
        //    if (mat.Empty())
        //        return null;
        //    window.ShowImage(mat);
        //    Cv2.WaitKey(30);
        //    var converted = mat.ToBitmap(PixelFormat.Format32bppArgb);
        //    return new Bitmap(converted, size);
        //}

        public void Stop()
        {
            isRunning = false;
            capture?.Dispose();
        }
    }
}
