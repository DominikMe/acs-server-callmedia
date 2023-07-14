using Azure.Communication.Calling.WindowsClient;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Foundation;

namespace AcsWindowsClient
{
    internal class VideoStreamer
    {
        public VirtualOutgoingVideoStream VideoStream { get; }
        public bool IsRunning { get; private set; }
        public BlockingCollection<MemoryBuffer> Queue { get; } = new();

        public VideoStreamer(VideoStreamFormat videoFormat)
        {
            VideoStream = new(new()
            {
                Formats = new[] { videoFormat }
            });
        }

        private Task SendFrameAsync(RawVideoFrame rawVideoFrame)
        {
            if (VideoStream is null || VideoStream.State != VideoStreamState.Started)
                return Task.CompletedTask;
            return VideoStream.SendRawVideoFrameAsync(rawVideoFrame).AsTask();
        }

        public void Start()
        {
            IsRunning = true;

            _ = Task.Run(Send);

            async Task Send()
            {
                Stopwatch sw = new();
                while (IsRunning)
                {
                    sw.Restart();
                    if (Queue.Count > 0)
                    {
                        var memoryBuffer = Queue.Take();
                        RawVideoFrameBuffer frame = new()
                        {
                            StreamFormat = VideoStream.Format,
                            Buffers = new[] { memoryBuffer }
                        };
                        await SendFrameAsync(frame);
                    }
                    var delay = (int)(1000 / VideoStream.Format.FramesPerSecond) - (int)sw.ElapsedMilliseconds;
                    if (delay > 2)
                        await Task.Delay(delay);
                }
            }
        }

        public void Stop() { IsRunning = false; }
    }
}
