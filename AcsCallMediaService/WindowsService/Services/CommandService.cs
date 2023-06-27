using Grpc.Core;
using GrpcProto;
using MemoryMappedFiles;
using System.Drawing;
using System.Threading.Channels;

namespace WindowsService.Services
{
	public class CommandService : ServerSentCommands.ServerSentCommandsBase
	{
		private string callToken = "";
		private string displayName = "VideoBridge";
		private string meetingJoinUrl = "";


		public override async Task GetCommands(
			GetCommandsRequest request,
			IServerStreamWriter<Command> responseStream,
			ServerCallContext context)
		{
			var channel = Channel.CreateBounded<(Command command, int waitAfter)>(new BoundedChannelOptions(capacity: 5));

			var consumerTask = Task.Run(async () =>
			{
				// Consume messages from channel and write to response stream.
				await foreach (var (command, waitAfter) in channel.Reader.ReadAllAsync())
				{
					await responseStream.WriteAsync(command);
					await Task.Delay(waitAfter);
				}
			});

			Command joinCommand = new() { Action = "JoinTeamsMeeting" };
			joinCommand.Args.AddRange(new[] { callToken, displayName, meetingJoinUrl });

			await channel.Writer.WriteAsync((joinCommand, 5000)); // actually need to wait until connected

			// send frames

			Size size = new() { Width = 1280, Height = 720 };
			int fps = 30;
			int position = 0;
			while (true)
			{
				Bitmap bitmap = TestFrameGenerator.CreateFrame(size, position, 20);
				position = (position + 1) % size.Width;
				string memFile = $"v{DateTimeOffset.UtcNow.Ticks}";
				await MemFileIO.WriteBitmapToMemoryMappedFile(bitmap, memFile);
				Command command = new() { Action = "SendVideoFrame" };
				command.Args.AddRange(new[] { displayName, meetingJoinUrl, memFile });

				await channel.Writer.WriteAsync((command, 1000 / fps));
			}

			//channel.Writer.Complete();
			//await consumerTask;
		}
	}
}
