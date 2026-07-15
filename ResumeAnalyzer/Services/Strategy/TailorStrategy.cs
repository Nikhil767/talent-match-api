using ResumeAnalyzer.Services.Providers;

namespace ResumeAnalyzer.Services.Strategy
{
	public class TailorStrategy(
	GeminiService gemini,
	GroqService groq
	//,OpenAiMiniService openai
	) : ITailorStrategy
	{
		public async Task<string> TailorResumeAsync(string resumeText, string jobDescription)
		{
			try
			{
				return await gemini.TailorResumeAsync(resumeText, jobDescription);
			}
			catch
			{
				try
				{
					return await groq.TailorResumeAsync(
	"""
You are an expert technical resume writer. Optimize the provided RESUME to align with the core requirements of the JOB DESCRIPTION without fabricating experience.
CRITICAL: Return ONLY a valid JSON object matching the schema below. No markdown wrapping (like ```json), and no conversational intro/outro text.
{"tailored_summary":"A rewritten 3-sentence professional summary matching the target job keywords.","tailored_bullets":["Rewrite 4-6 high-impact bullet points from the original resume, optimizing them to emphasize achievements relevant to the JD."],"keywords_added":["List of core technical skills from the JD successfully woven into the resume text."],"original_preserved":["List 2-3 primary company names or roles verified and locked down to ensure factual integrity."]}
""",
	$"RESUME:\n{resumeText}\n\nJOB:\n{jobDescription}"
	);
				}
				catch
				{
					throw;
				}
			}
		}
	}
}
