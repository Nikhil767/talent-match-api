using ResumeAnalyzer.Domain.Dto;

namespace ResumeAnalyzer.Services.Facade
{
	public interface IAnalysisPipelineService
	{
		Task<object?> AnalyzeAtsAsync(AtsRequestDto req, Guid userId);
		Task<object?> AnalyzeGapsAsync(GapRequestDto req, Guid userId);
		Task<object?> TailorResumeAsync(TailorRequestDto req, Guid userId);
	}
}
