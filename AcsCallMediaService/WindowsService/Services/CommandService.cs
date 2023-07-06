using Grpc.Core;
using GrpcProto;
using System.Threading.Channels;

namespace WindowsService.Services
{
    public class CommandService : ServerSentCommands.ServerSentCommandsBase
	{
		private string callToken = "eyJhbGciOiJSUzI1NiIsImtpZCI6IjVFODQ4MjE0Qzc3MDczQUU1QzJCREU1Q0NENTQ0ODlEREYyQzRDODQiLCJ4NXQiOiJYb1NDRk1kd2M2NWNLOTVjelZSSW5kOHNUSVEiLCJ0eXAiOiJKV1QifQ.eyJza3lwZWlkIjoiYWNzOmZjZjJmZTIwLTFlNDAtNDI0Ny04YWFlLWY0ZTk0YzdmY2NmOF8wMDAwMDAxOS1iYmJjLWQxYWUtNjMzMS04ZTNhMGQwMGMzYTciLCJzY3AiOjE3OTIsImNzaSI6IjE2ODg1ODAxMDciLCJleHAiOjE2ODg1ODM3MDcsInJnbiI6ImFtZXIiLCJhY3NTY29wZSI6InZvaXAiLCJyZXNvdXJjZUlkIjoiZmNmMmZlMjAtMWU0MC00MjQ3LThhYWUtZjRlOTRjN2ZjY2Y4IiwicmVzb3VyY2VMb2NhdGlvbiI6InVuaXRlZHN0YXRlcyIsImlhdCI6MTY4ODU4MDEwN30.eFK7IfXk1wYs9n4hnw5dx3puplc5efDHw-uZLgHTGrrXST-e2tJcbDJ6S3po1LN8yqI0HUFtipQ2lqFZJUS48XR6rUav1lx_QRrlc50B1zhirymrS_wF9Hn-kK86lgxYnxtam4yM_wSLK0bLRDMdSaWlTxXUv2MWQhIwQ9SQ104U54Uu-bQ57s-g55qPn2rplGUxcBQ60HcUvrEplFGQO843_2xV_2S15qjdh-6I-80Qnvn2irZKLcp1sYvPjFwslAx5N_zCvSBSrHOWdW3QKzFGX1g-nnxdA1U9MO9I5OQcFsHGoh4OAZsA0hGyYXpVVMkEjqBgVepsCXzSi9xOiQ";
		private string displayName = "VideoBridge";
		private string meetingJoinUrl = "https://teams.microsoft.com/l/meetup-join/19%3ameeting_MTk4NmVlMjMtYTRlNy00NDUwLThmNDgtZTU5OGRlMTM0Mjdi%40thread.v2/0?context=%7b%22Tid%22%3a%2272f988bf-86f1-41af-91ab-2d7cd011db47%22%2c%22Oid%22%3a%22bf45fc88-2e9a-4aaf-a32e-bbfb3910ba05%22%7d";
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
