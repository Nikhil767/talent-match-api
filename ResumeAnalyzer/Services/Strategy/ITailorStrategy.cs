namespace ResumeAnalyzer.Services.Strategy
{
	public interface ITailorStrategy
	{
		Task<string> TailorResumeAsync(string resumeText, string jobDescription);
	}
}
