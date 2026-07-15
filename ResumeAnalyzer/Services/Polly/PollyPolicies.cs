using Polly;
using Polly.Extensions.Http;
using Polly.Retry;

namespace ResumeAnalyzer.Services.Polly
{
	public class PollyPolicies
	{
		public AsyncRetryPolicy<HttpResponseMessage> RetryPolicy =>
		HttpPolicyExtensions
			.HandleTransientHttpError()
			.WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromMilliseconds(200 * retryAttempt));
	}
}
