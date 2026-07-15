using System.Threading.Channels;

namespace ResumeAnalyzer.Background
{
	public class AnalysisQueue
	{
		// Bound to a max capacity to protect memory on free hosting
		private readonly Channel<(Guid recordId, string bearerToken)> _queue = Channel.CreateBounded<(Guid recordId, string bearerToken)>(new BoundedChannelOptions(100)
		{
			FullMode = BoundedChannelFullMode.Wait
		});

		public ValueTask QueueBackgroundWorkItemAsync(Guid recordId, string bearerToken) => _queue.Writer.WriteAsync((recordId, bearerToken));

		public ValueTask<(Guid recordId, string bearerToken)> DequeueAsync(CancellationToken cancellationToken) => _queue.Reader.ReadAsync(cancellationToken);
	}
}
