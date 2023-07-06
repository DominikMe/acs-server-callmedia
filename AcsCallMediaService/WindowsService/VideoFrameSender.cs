using GrpcProto;
using MemoryMappedFiles;
using System.Drawing;
using WindowsService.Services;

namespace WindowsService
{
    public class VideoFrameSender
    {
        private readonly CommandService commandService;
        private readonly string displayName;
        private readonly string meetingJoinUrl;
        private volatile bool isRunning;

        public VideoFrameSender(CommandService commandService, string displayName, string meetingJoinUrl)
        {
            this.commandService = commandService;
            this.displayName = displayName;
            this.meetingJoinUrl = meetingJoinUrl;
        }

        public async Task Start()
        {
            if (isRunning) return;

            isRunning = true;
            int position = 0;
            Size size = new() { Width = 1280, Height = 720 };
            int fps = 30;
            while (isRunning)
            {
                await SendFrame(position, size);
                position = (position + 1) % size.Width;
                await Task.Delay(1000 / fps);
            }
            
            async ValueTask SendFrame(int position, Size size)
            {
                try
                {
                    Bitmap bitmap = TestFrameGenerator.CreateFrame(size, position, 20);
                    string memFile = $"v{DateTimeOffset.UtcNow.Ticks}";
                    await MemFileIO.WriteBitmapToMemoryMappedFile(bitmap, memFile);
                    Command command = new() { Action = "SendVideoFrame" };
                    command.Args.AddRange(new[] { displayName, meetingJoinUrl, memFile });

                    await commandService.Commands.Writer.WriteAsync(command);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
        }
    }

        public void Stop()
        {
            isRunning = false;
        }
    }
}
