using GrpcProto;
using MemoryMappedFiles;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AcsWindowsClient
{
    internal class CommandHandler
    {
        private Dictionary<string, CallHandler> callHandlers = new();

        public CommandHandler() { }

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
        }

        private async Task SendVideoFrame(string displayName, string call, string memFile)
        {
            CallHandler callHandler = callHandlers[$"{displayName}@{call}"];
            // should we do the IO here or in VideoStreamer?
            var bitmap = await MemFileIO.ReadBitmapFromMemoryMappedFile(memFile, new() { Width = 1280, Height = 720 }); // todo don't hardcode size here
            callHandler.EnqueueVideoFrame(bitmap.ToMemoryBuffer());
            // todo: delete memFile
        }
    }
}
