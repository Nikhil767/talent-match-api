using ResumeAnalyzer.Domain.Dto;

namespace ResumeAnalyzer.Services.Facade
{
	public interface IResumeService
	{
		Task<ResumeExportResultDto> ExportAsync(ResumeExportRequestDto req);
	}
}
