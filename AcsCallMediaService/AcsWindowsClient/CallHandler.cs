using Azure.Communication.Calling.WindowsClient;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Foundation;

namespace AcsWindowsClient
{
    internal class CallHandler
    {
        private readonly string token;
        private readonly string displayName;
        private CallClient callClient;
        private CallAgent callAgent;
        private CommunicationCall call;
        private VideoStreamer videoStreamer;

        public CallHandler(string token, string displayName)
        {
            this.token = token;
            this.displayName = displayName;
        }

        public async Task Initialize()
        {
            callClient = new CallClient();
            callAgent = await callClient.CreateCallAgentAsync(new CallTokenCredential(token), new CallAgentOptions
            {
                DisplayName = displayName
            }).AsTask();
        }

        public async Task JoinTeamsMeeting(string meetingJoinUrl)
        {
            videoStreamer = new(GetDefaultVideoFormat());
            call = await callAgent.JoinAsync(new TeamsMeetingLinkLocator(meetingJoinUrl), new JoinCallOptions()
            {
                OutgoingVideoOptions = new()
                {
                    Streams = new[] { videoStreamer.VideoStream }
                }
            });
            call.StateChanged += (_, __) =>
            {
                if (call.State == CallState.Connected)
                {
                    videoStreamer.Start();
                }
                else
                {
                    videoStreamer.Stop();
                }
            };
        }

        public void EnqueueVideoFrame(MemoryBuffer memBuffer)
        {
            videoStreamer.Queue.Add(memBuffer);
        }

        private VideoStreamFormat GetDefaultVideoFormat() => new()
        {
            Resolution = VideoStreamResolution.Hd,
            PixelFormat = VideoStreamPixelFormat.Rgba,
            FramesPerSecond = 30,
            Stride1 = 1280 * 4 // It is times 4 because RGBA is a 32-bit format.
        };
    }
}
