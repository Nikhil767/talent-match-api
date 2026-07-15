using System.Text.Json;

namespace ResumeAnalyzer.Services.Providers
{
	public class GeminiService(IHttpClientFactory httpFactory, ILogger<GeminiService> logger)
	{
		public async Task<string> TailorResumeAsync(string resumeText, string jobDescription)
		{
			try
			{
				var client = httpFactory.CreateClient("gemini");
				var bodyValue = new
				{
					systemInstruction = new
					{
						parts = new[] { new { text = """
You are an expert technical resume writer. Optimize the provided RESUME to align with the core requirements of the JOB DESCRIPTION without fabricating experience.
CRITICAL: Return ONLY a valid JSON object matching the schema below. No markdown wrapping (like ```json), and no conversational intro/outro text.
{"tailored_summary":"A rewritten 3-sentence professional summary matching the target job keywords.","tailored_bullets":["Rewrite 4-6 high-impact bullet points from the original resume, optimizing them to emphasize achievements relevant to the JD."],"keywords_added":["List of core technical skills from the JD successfully woven into the resume text."],"original_preserved":["List 2-3 primary company names or roles verified and locked down to ensure factual integrity."]}
""" } }
					},
					contents = new[]
					{
						new {
							role = "user",
							parts = new[] { new { text = $"RESUME:\n{resumeText}\n\nJOB:\n{jobDescription}" } }
						}
					},
					generationConfig = new { temperature = 0.2, response_mime_type = "application/json" }
				};
				var res = await client.PostAsJsonAsync("models/gemini-2.5-flash:generateContent", bodyValue);
				//var res = await client.PostAsJsonAsync("models/gemini-1.5-flash:generateContent?key=" + _key, bodyValue);
				if (!res.IsSuccessStatusCode)
				{
					var body = await res.Content.ReadAsStringAsync();
					logger.LogWarning("Gemini call failed {Status}: {Body}", res.StatusCode, body);
				}
				res.EnsureSuccessStatusCode();

				var json = await res.Content.ReadFromJsonAsync<JsonElement>();
				return json.GetProperty("candidates")[0]
						   .GetProperty("content")
						   .GetProperty("parts")[0]
						   .GetProperty("text")
						   .GetString()!;
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "Gemini TailorResumeAsync exception");
				throw;
			}
		}

		public async Task<string> CallAsync(string promptText)
		{
			try
			{
				var client = httpFactory.CreateClient("gemini");				
				var bodyValue = new { contents = new { parts = new[] { new { text = promptText } } }, generationConfig = new { response_mime_type = "application/json" } };
				var res = await client.PostAsJsonAsync("models/gemini-2.5-flash:generateContent", bodyValue);
				if (!res.IsSuccessStatusCode)
				{
					var body = await res.Content.ReadAsStringAsync();
					logger.LogWarning("Gemini call failed {Status}: {Body}", res.StatusCode, body);
				}
				res.EnsureSuccessStatusCode();

				var json = await res.Content.ReadFromJsonAsync<JsonElement>();
				return json.GetProperty("candidates")[0]
						   .GetProperty("content")
						   .GetProperty("parts")[0]
						   .GetProperty("text")
						   .GetString()!;
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "Gemini CallAsync exception");
				throw;
			}
		}

		public async Task<string> ParseAndAnalyzeResumeAsync(string resumeText)
		{
			var promptText = """
You are an expert resume analyzer and parser. Process the resume and return ONLY a valid, unified JSON object matching this schema:
{"skills":[],"ats_score":0,"summary":""}
""";
			promptText += $"\nResume:\n{resumeText}";
			return await CallAsync(promptText);
		}

		public async Task<string> GetAtsScoreAsync(string resumeText, string jobDescription)
		{
			var combined = $"\nRESUME:\n{resumeText}\n\nJOB:\n{jobDescription}";
			var promptText = """
You are an expert Applicant Tracking System (ATS) and resume parser.
Analyze the provided RESUME against the JOB.
CRITICAL: You must return ONLY a single, valid, unified JSON object. Do not include markdown formatting like ```json ... ``` or any trailing text. Follow this schema exactly:
{"ats_score":0,"experience_years":0,"keyword_hits":[],"keyword_misses":[],"format_issues":[],"strengths":[],"weaknesses":[],"suggestions":[]}
""";
			promptText += combined;
			return await CallAsync(promptText);
		}

		public async Task<string> GetGapAnalysisAsync(string resumeText, string jobDescription)
		{
			var combined = $"\nRESUME:\n{resumeText}\n\nJOB:\n{jobDescription}";
			var promptText = """
You are an expert technical recruiter and resume strategist.
Perform a comprehensive Gap Analysis comparing the candidate's RESUME against the requirements of the JOB.
Identify missing skills, critical tech stack components, certifications, or experience level gaps.
CRITICAL: Return ONLY a valid JSON object matching this schema. Do not output markdown code blocks or conversational text.
{"match_percentage":0,"missing_hard_skills":[],"missing_soft_skills":[],"experience_delta":"Describe any missing domain knowledge or gaps in seniority levels.","high_priority_gaps":["List 2-4 critical things required by the JD that the candidate completely lacks."],"upskilling_recommendations":["Actionable recommendations for projects, certs, or keywords the user should add to close these gaps."]}
""";
			promptText += combined;
			return await CallAsync(promptText);
		}

		public async Task<string> ExplainMatchAsync(string resumeText, string jobDescription)
		{
			var combined = $"\nRESUME:\n{resumeText}\n\nJOB:\n{jobDescription}";
			var promptText = """
You are a professional career coach and HR analyst.
Evaluate how well the candidate's RESUME matches the JOB and generate an explanatory alignment breakdown.
CRITICAL: Return ONLY a valid JSON object matching this schema. Do not output markdown code blocks or conversational text.
{ "match_score":0, "top_reasons":[], "concerns":[],"match_summary": "A 2-3 sentence high-level narrative explaining the overall alignment of the candidate to this role."}
""";
			promptText += combined;
			return await CallAsync(promptText);
		}

	}
}
