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
            switch (command.CommandCase)
            {
                case Command.CommandOneofCase.JoinTeamsMeeting:
                    await JoinTeamsMeeting(command.JoinTeamsMeeting);
                    break;
                case Command.CommandOneofCase.SendVideoFrame:
                    await SendVideoFrame(command.SendVideoFrame);
                    break;
            }
        }

        private async Task JoinTeamsMeeting(JoinTeamsMeeting joinCommand)
        {
            string displayName = joinCommand.DisplayName;
            string meetingJoinUrl = joinCommand.MeetingJoinUrl;
            CallHandler callHandler = new(joinCommand.CallToken, displayName);
            callHandlers[$"{displayName}@{meetingJoinUrl}"] = callHandler;
            await callHandler.Initialize();
            await callHandler.JoinTeamsMeeting(meetingJoinUrl);
            await eventsClient.HasJoinedAsync(new HasJoinedRequest { DisplayName = displayName, MeetingJoinUrl = meetingJoinUrl });
        }

        private async Task SendVideoFrame(SendVideoFrame sendVideoCommand)
        {
            CallHandler callHandler = callHandlers[$"{sendVideoCommand.DisplayName}@{sendVideoCommand.CallLocator}"];
            // should we do the IO here or in VideoStreamer?
            var bitmap = await MemFileIO.ReadBitmapFromMemoryMappedFile(sendVideoCommand.MemoryMappedFileName, new() { Width = 1280, Height = 720 }, disposeAfter: true); // todo don't hardcode size here

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
