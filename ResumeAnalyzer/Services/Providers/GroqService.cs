using System.Text.Json;

namespace ResumeAnalyzer.Services.Providers
{
	public class GroqService(IHttpClientFactory httpFactory, ILogger<GroqService> logger)
	{
		/// <summary>
		/// gap
		/// explainmatch
		/// </summary>
		/// <param name="systemPrompt"></param>
		/// <param name="userContent"></param>
		/// <returns></returns>
		public async Task<string> ExecuteReasoningTaskAsync(string systemPrompt, string userContent)
		{
			var client = httpFactory.CreateClient("groq");
			var req = new HttpRequestMessage(HttpMethod.Post, "chat/completions");
			//req.Headers.Add("Authorization", $"Bearer {_key}");
			// gpt-oss-20b excels at deep logic but needs reasoning_format explicitly handled
			req.Content = JsonContent.Create(new
			{
				model = "openai/gpt-oss-20b",
				messages = new[]
				{
			new { role = "system", content = systemPrompt },
			new { role = "user", content = userContent }
		},
				temperature = 0.1,
				response_format = new { type = "json_object" },
				reasoning_format = "hidden" // Keeps internal thoughts out of the final JSON output string
			});
			var res = await client.SendAsync(req);
			if (!res.IsSuccessStatusCode)
			{
				var body = await res.Content.ReadAsStringAsync();
				logger.LogWarning("Groq reasoning call failed {Status}: {Body}", res.StatusCode, body);
			}
			res.EnsureSuccessStatusCode();
			var json = await res.Content.ReadFromJsonAsync<JsonElement>();
			return json.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString()!;
		}

		/// <summary>
		/// ats
		/// tailorresume
		/// </summary>
		/// <param name="systemPrompt"></param>
		/// <param name="userContent"></param>
		/// llama3-70b-8192 - not working
		/// llama-3.3-70b-specdec
		/// llama-3.3-70b-versatile
		/// <returns></returns>
		public async Task<string> ExecuteStructuralTaskAsync(string systemPrompt, string userContent)
		{
			var client = httpFactory.CreateClient("groq");
			var req = new HttpRequestMessage(HttpMethod.Post, "chat/completions");
			//req.Headers.Add("Authorization", $"Bearer {_key}");
			req.Content = JsonContent.Create(new
			{
				model = "llama-3.3-70b-versatile",
				messages = new[]
				{
			new { role = "system", content = systemPrompt },
			new { role = "user", content = userContent }
		},
				temperature = 0.1,
				response_format = new { type = "json_object" }
			});
			var res = await client.SendAsync(req);
			if (!res.IsSuccessStatusCode)
			{
				var body = await res.Content.ReadAsStringAsync();
				logger.LogWarning("Groq structural call failed {Status}: {Body}", res.StatusCode, body);
			}
			res.EnsureSuccessStatusCode();
			var json = await res.Content.ReadFromJsonAsync<JsonElement>();
			return json.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString()!;
		}


		private async Task<string> CallAsync(string systemPrompt, string userContent)
		{
			try
			{
				var client = httpFactory.CreateClient("groq");
				var req = new HttpRequestMessage(HttpMethod.Post, "chat/completions");
				//req.Headers.Add("Authorization", $"Bearer {_key}");
				req.Content = JsonContent.Create(new
				{
					model = "llama3-70b-8192",
					messages = new[] { new { role = "system", content = systemPrompt }, new { role = "user", content = userContent } },
					temperature = 0.1,
					response_format = new { type = "json_object" }
				});

				var res = await client.SendAsync(req);
				if (!res.IsSuccessStatusCode)
				{
					var body = await res.Content.ReadAsStringAsync();
					logger.LogWarning("Groq call failed {Status}: {Body}", res.StatusCode, body);
				}
				res.EnsureSuccessStatusCode();

				var json = await res.Content.ReadFromJsonAsync<JsonElement>();
				return json.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString()!;
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "Groq CallAsync exception");
				throw;
			}
		}

		public Task<string> ParseAndAnalyzeResumeAsync(string resumeText) 
		{
//			var sp = """
//You are an expert resume analyzer and parser. Process the resume and return ONLY a valid, unified JSON object matching this schema:
//{"parsed_data":{"skills":[],"experience_years":0,"education":[],"job_titles":[],"certifications":[],"summary":""},"ats_evaluation":{"ats_score":0,"keyword_hits":[],"keyword_misses":[],"format_issues":[],"strengths":[],"weaknesses":[],"suggestions":[]}}
//""";
			return ExecuteStructuralTaskAsync("""
You are an expert resume analyzer and parser. Process the resume and return ONLY a valid, unified JSON object matching this schema:
{"skills":[],"ats_score":0,"summary":""}
""", resumeText);
		}

		public Task<string> GetAtsScoreAsync(string resumeText, string jobDescription) =>
			ExecuteStructuralTaskAsync("""
            You are an expert Applicant Tracking System (ATS) and resume parser.
        Analyze the provided RESUME against the JOB.
        CRITICAL: You must return ONLY a single, valid, unified JSON object. Do not include markdown formatting like ```json ... ``` or any trailing text. Follow this schema exactly:
        {"ats_score":0,"experience_years":0,"keyword_hits":[],"keyword_misses":[],"format_issues":[],"strengths":[],"weaknesses":[],"suggestions":[]}
        """, $"RESUME:\n{resumeText}\n\nJOB:\n{jobDescription}");

		public Task<string> GetGapAnalysisAsync(string resumeText, string jobDescription) =>
			ExecuteReasoningTaskAsync("""
            You are an expert technical recruiter and resume strategist.
        Perform a comprehensive Gap Analysis comparing the candidate's RESUME against the requirements of the JOB.
        Identify missing skills, critical tech stack components, certifications, or experience level gaps.
        CRITICAL: Return ONLY a valid JSON object matching this schema. Do not output markdown code blocks or conversational text.
            {"match_percentage":0,"missing_hard_skills":[],"missing_soft_skills":[],"experience_delta":"Describe any missing domain knowledge or gaps in seniority levels.","high_priority_gaps":["List 2-4 critical things required by the JD that the candidate completely lacks."],"upskilling_recommendations":["Actionable recommendations for projects, certs, or keywords the user should add to close these gaps."]}
        """, $"RESUME:\n{resumeText}\n\nJOB:\n{jobDescription}");

		public Task<string> ExplainMatchAsync(string resumeText, string jobDescription) =>
			ExecuteReasoningTaskAsync("""
You are a professional career coach and HR analyst.
Evaluate how well the candidate's RESUME matches the JOB and generate an explanatory alignment breakdown.
CRITICAL: Return ONLY a valid JSON object matching this schema. Do not output markdown code blocks or conversational text.
{ "match_score":0, "top_reasons":[], "concerns":[],"match_summary": "A 2-3 sentence high-level narrative explaining the overall alignment of the candidate to this role."}
""", $"RESUME:\n{resumeText}\n\nJOB:\n{jobDescription}");

		public Task<string> TailorResumeAsync(string systemPrompt, string userContent) =>
			ExecuteStructuralTaskAsync(systemPrompt, userContent);
	}
}
