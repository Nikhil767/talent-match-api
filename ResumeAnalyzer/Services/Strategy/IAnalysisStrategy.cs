namespace ResumeAnalyzer.Services.Strategy
{
	public interface IAnalysisStrategy
	{
		Task<string> ExtractSkillsAsync(string resumeText);
		Task<string> GetAtsScoreAsync(string resumeText, string jobDescription);
		Task<string> GetGapAnalysisAsync(string resumeText, string jobDescription);
		Task<string> ExplainMatchAsync(string resumeText, string jobDescription);
	}
}
