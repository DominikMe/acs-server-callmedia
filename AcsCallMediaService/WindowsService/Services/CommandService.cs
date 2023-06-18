using Grpc.Core;
using GrpcProto;
using System.Threading.Channels;

namespace WindowsService.Services
{
	public class CommandService : ServerSentCommands.ServerSentCommandsBase
	{
		public override async Task GetCommands(
			GetCommandsRequest request,
			IServerStreamWriter<Command> responseStream,
			ServerCallContext context)
		{
			var channel = Channel.CreateBounded<Command>(new BoundedChannelOptions(capacity: 5));

			var consumerTask = Task.Run(async () =>
			{
				// Consume messages from channel and write to response stream.
				await foreach (var message in channel.Reader.ReadAllAsync())
				{
					await responseStream.WriteAsync(message);
				}
			});

			var message = new Command { Action = "JoinCall" };
			message.Args.AddRange(new[] { "call1", "video" });
			await channel.Writer.WriteAsync(message);

			// Complete writing and wait for consumer to complete.
			channel.Writer.Complete();
			await consumerTask;
		}
	}
}
