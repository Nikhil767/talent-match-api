using ResumeAnalyzer.Domain.Dto;

namespace ResumeAnalyzer.Services.Facade
{
	public interface IJobPipelineService
	{
		Task<object> IngestJobsAsync(JobSearchRequestDto reqDto, CancellationToken ct = default);
		Task<object?> MatchJobsToResumeAsync(MatchRequestDto req, Guid userId, CancellationToken ct = default);
	}
}
