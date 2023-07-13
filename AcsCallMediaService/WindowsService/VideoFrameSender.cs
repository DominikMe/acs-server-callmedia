using GrpcProto;
using MemoryMappedFiles;
using System.Drawing;
using System.Threading.Channels;

namespace WindowsService
{
    public class VideoFrameSender
    {
        private readonly ChannelWriter<Command> commandWriter;
        private readonly string displayName;
        private readonly string meetingJoinUrl;
        private readonly string? testVideoStream;
        private readonly string channelId;
        private volatile bool isRunning;

        public VideoFrameSender(ChannelWriter<Command> commandWriter, string displayName, string meetingJoinUrl, string? testVideoStream)
        {
            this.commandWriter = commandWriter;
            this.displayName = displayName;
            this.meetingJoinUrl = meetingJoinUrl;
            this.testVideoStream = testVideoStream;
            channelId = Guid.NewGuid().ToString();
        }

        public async Task Start()
        {
            if (isRunning) return;

            isRunning = true;
            Size size = new() { Width = 1280, Height = 720 };
            int fps = 30;

            if (testVideoStream is null)
            {
                await SendTestFrames();
            }
            else
            {
                RtspInput.RtspInput rtsp = new(testVideoStream, size, fps, SendFrame);
                await rtsp.Start();
            }

            async ValueTask SendTestFrames()
            {
                int position = 0;
                while (isRunning)
                {
                    await SendFrame(TestFrameGenerator.CreateFrame(size, position, 20));
                    position = (position + 1) % size.Width;
                    await Task.Delay(1000 / fps);
                }
            }

            async ValueTask SendFrame(Bitmap bitmap)
            {
                string memFile = $"{channelId}_v{DateTimeOffset.UtcNow.Ticks}";
                await MemFileIO.WriteBitmapToMemoryMappedFile(bitmap, memFile);

                Command command = new()
                {
                    SendVideoFrame = new()
                    {
                        DisplayName = displayName,
                        CallLocator = meetingJoinUrl,
                        MemoryMappedFileName = memFile
                    }
                };

                await commandWriter.WriteAsync(command);
            }
        }

        public void Stop()
        {
            isRunning = false;
        }
    }
}
