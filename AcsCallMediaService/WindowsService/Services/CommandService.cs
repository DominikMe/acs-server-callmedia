using Grpc.Core;
using GrpcProto;
using System.Threading.Channels;

namespace WindowsService.Services
{
    public class CommandService : ServerSentCommands.ServerSentCommandsBase
	{
		private string callToken = "";
		private string displayName = "VideoBridge";
		private string meetingJoinUrl = "";
        private readonly EventsService eventsService;

        public Channel<Command> Commands { get; private set; } = Channel.CreateBounded<Command>(new BoundedChannelOptions(capacity: 1));

        public CommandService(EventsService eventsService)
		{
            this.eventsService = eventsService;
        }
		
		public override async Task GetCommands(
			GetCommandsRequest request,
			IServerStreamWriter<Command> responseStream,
			ServerCallContext context)
		{
			var consumerTask = Task.Run(async () =>
			{
				// Consume messages from channel and write to response stream.
				await foreach (var command in Commands.Reader.ReadAllAsync())
				{
					await responseStream.WriteAsync(command);
				}
			});

			Command joinCommand = new() { Action = "JoinTeamsMeeting" };
			joinCommand.Args.AddRange(new[] { callToken, displayName, meetingJoinUrl });

			await Commands.Writer.WriteAsync(joinCommand);
			// actually need to wait until connected, not just 5sec
			await eventsService.WaitUntilHasJoined(meetingJoinUrl, displayName);

			await new VideoFrameSender(this, displayName, meetingJoinUrl).Start();
		}
	}
}
