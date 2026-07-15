//using System.Text.Json;

//namespace ResumeAnalyzer.Services.Providers
//{
//	public class OpenAiMiniService(IConfiguration config, IHttpClientFactory httpFactory, ILogger<OpenAiMiniService> logger)
//	{
//		private readonly string _key = config["OpenAI:ApiKey"]!;

//		private async Task<string> AnalyzeAsync(string systemPrompt, string userContent)
//		{
//			try
//			{
//				var client = httpFactory.CreateClient("openai");
//				var req = new HttpRequestMessage(HttpMethod.Post, "chat/completions");
//				req.Headers.Add("Authorization", $"Bearer {_key}");
//				req.Content = JsonContent.Create(new
//				{
//					model = "o3-mini",
//					messages = new[]
//					{
//				new { role = "system", content = systemPrompt },
//				new { role = "user",   content = userContent }
//			},
//					temperature = 0.1,
//					response_format = new { type = "json_object" }
//				});

//				var res = await client.SendAsync(req);
//				if (!res.IsSuccessStatusCode)
//				{
//					var body = await res.Content.ReadAsStringAsync();
//					logger.LogWarning("OpenAi call failed {Status}: {Body}", res.StatusCode, body);
//				}
//				res.EnsureSuccessStatusCode();

//				var json = await res.Content.ReadFromJsonAsync<JsonElement>();
//				return json.GetProperty("choices")[0]
//						   .GetProperty("message")
//						   .GetProperty("content")
//						   .GetString()!;
//			}
//			catch (Exception ex)
//			{
//				logger.LogError(ex, "OpenAi CallAsync exception");
//				throw;
//			}
//		}
//	}
//}
