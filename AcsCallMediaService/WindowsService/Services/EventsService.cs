using Grpc.Core;
using GrpcProto;

namespace WindowsService.Services
{
	public class EventsService : Events.EventsBase
	{
		private readonly ILogger<ReceiverService> _logger;
		public EventsService(ILogger<ReceiverService> logger)
		{
			_logger = logger;
		}

		private Dictionary<string, TaskCompletionSource> hasJoinedCompletions = new();

		public Task WaitUntilHasJoined(string meetingJoinUrl, string displayName)
		{
			string key = $"{meetingJoinUrl}:{displayName}";
			return GetTaskCompletionSource(hasJoinedCompletions, key).Task;
		}

		private TaskCompletionSource GetTaskCompletionSource(Dictionary<string, TaskCompletionSource> dict, string key)
		{
            if (!dict.ContainsKey(key))
            {
                dict.Add(key, new TaskCompletionSource());
            }
            return dict[key];
        }

		public override Task<HasJoinedResponse> HasJoined(HasJoinedRequest request, ServerCallContext context)
		{
            string key = $"{request.MeetingJoinUrl}:{request.DisplayName}";
			GetTaskCompletionSource(hasJoinedCompletions, key).SetResult();
			return Task.FromResult(new HasJoinedResponse());
		}
	}
}