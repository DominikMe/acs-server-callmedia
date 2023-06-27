using Grpc.Core;
using GrpcProto;

namespace WindowsService.Services
{
	public class ReceiverService : Receiver.ReceiverBase
	{
		private readonly ILogger<ReceiverService> _logger;
		public ReceiverService(ILogger<ReceiverService> logger)
		{
			_logger = logger;
		}

		public override Task<ReceiveResponse> ReceiveFrame(ReceiveRequest request, ServerCallContext context)
		{
			return Task.FromResult(new ReceiveResponse
            {
				Message = "Hello " + request.Name
			});
		}
	}
}