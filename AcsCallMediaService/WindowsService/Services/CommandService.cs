using Grpc.Core;
using GrpcProto;
using System.Threading.Channels;

namespace WindowsService.Services
{
    public class CommandService : ServerSentCommands.ServerSentCommandsBase
	{
		private const string displayName = "VideoBridge";
        private readonly EventsService eventsService;
        private readonly IConfiguration configuration;

        public Channel<Command> Commands { get; private set; } = Channel.CreateBounded<Command>(new BoundedChannelOptions(capacity: 1));

        public CommandService(EventsService eventsService, IConfiguration configuration)
		{
            this.eventsService = eventsService;
            this.configuration = configuration;
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

			string meetingJoinUrl = configuration["TestMeetingJoinUrl"]!;

            Command joinCommand = new()
			{
				JoinTeamsMeeting = new()
				{
					CallToken = configuration["AcsToken"],
					DisplayName = displayName,
					MeetingJoinUrl = meetingJoinUrl
                }
			};

			await Commands.Writer.WriteAsync(joinCommand);
			await eventsService.WaitUntilHasJoined(meetingJoinUrl, displayName);

			await new VideoFrameSender(Commands.Writer, displayName, meetingJoinUrl, configuration["TestVideoStream"]).Start();
		}
	}
}
