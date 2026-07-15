using ResumeAnalyzer.Services.Providers;

namespace ResumeAnalyzer.Services.Strategy
{
	public class AnalysisStrategy(
	GroqService groq,
	GeminiService geminiService
	//OpenAiMiniService openai
	) : IAnalysisStrategy
	{
		public async Task<string> ExtractSkillsAsync(string resumeText)
		{
			try
			{
				return await geminiService.ParseAndAnalyzeResumeAsync(resumeText);
			}
			catch
			{
				return await groq.ParseAndAnalyzeResumeAsync(resumeText);
			}
		}

		public async Task<string> GetAtsScoreAsync(string resumeText, string jobDescription)
		{
			try
			{
				return await geminiService.GetAtsScoreAsync(resumeText, jobDescription);				
			}
			catch
			{
				return await groq.GetAtsScoreAsync(resumeText, jobDescription);
			}
		}

		public async Task<string> GetGapAnalysisAsync(string resumeText, string jobDescription)
		{
			try
			{
				return await groq.GetGapAnalysisAsync(resumeText, jobDescription);
			}
			catch
			{
				return await geminiService.GetGapAnalysisAsync(resumeText, jobDescription);
			}
		}

		public async Task<string> ExplainMatchAsync(string resumeText, string jobDescription)
		{
			try
			{
				return await groq.ExplainMatchAsync(resumeText, jobDescription);
			}
			catch
			{
				return await geminiService.ExplainMatchAsync(resumeText, jobDescription);
			}
		}
	}
}
