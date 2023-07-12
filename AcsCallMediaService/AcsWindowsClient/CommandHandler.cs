using GrpcProto;
using MemoryMappedFiles;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Threading.Tasks;

namespace AcsWindowsClient
{
    internal class CommandHandler
    {
        private readonly Action<string> log;
        private readonly Events.EventsClient eventsClient;
        private readonly Action<Bitmap> onBitmapReceived;
        private Dictionary<string, CallHandler> callHandlers = new();

        public CommandHandler(Action<string> log, Events.EventsClient eventsClient, Action<Bitmap> onBitmapReceived)
        {
            this.log = log;
            this.eventsClient = eventsClient;
            this.onBitmapReceived = onBitmapReceived;
        }

        public async Task Handle(Command command)
        {
            switch (command.Action)
            {
                case "JoinTeamsMeeting":
                    await JoinTeamsMeeting(command.Args[0], command.Args[1], command.Args[2]);
                    break;
                case "SendVideoFrame":
                    await SendVideoFrame(command.Args[0], command.Args[1], command.Args[2]);
                    break;
            }
        }

        private async Task JoinTeamsMeeting(string token, string displayName, string meetingJoinUrl)
        {
            CallHandler callHandler = new(token, displayName);
            callHandlers[$"{displayName}@{meetingJoinUrl}"] = callHandler;
            await callHandler.Initialize();
            await callHandler.JoinTeamsMeeting(meetingJoinUrl);
            await eventsClient.HasJoinedAsync(new HasJoinedRequest { DisplayName = displayName, MeetingJoinUrl = meetingJoinUrl });
        }

        private async Task SendVideoFrame(string displayName, string call, string memFile)
        {
            CallHandler callHandler = callHandlers[$"{displayName}@{call}"];
            // should we do the IO here or in VideoStreamer?
            var bitmap = await MemFileIO.ReadBitmapFromMemoryMappedFile(memFile, new() { Width = 1280, Height = 720 }, disposeAfter: true); // todo don't hardcode size here

            if (bitmap.TestFirstPixel())
            {
                //log("Black frame!!!");
                Debug.WriteLine("Black frame!!");
            }
            else
            {
                onBitmapReceived(bitmap);
                callHandler.EnqueueVideoFrame(bitmap.ToMemoryBuffer());
            }
        }
    }
}
