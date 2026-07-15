using System.Text.Json;

namespace ResumeAnalyzer.Services.Providers
{
	public class EmbeddingService(IConfiguration config, IHttpClientFactory httpFactory, ILogger<EmbeddingService> logger)
	{
		//private readonly string _hfKey = config["HuggingFace:ApiKey"]!;
		//private readonly string _groqKey = config["Groq:ApiKey"]!;
		private readonly string _geminiKey = config["Gemini:ApiKey"]!;
		//private readonly string _openAiKey = config["OpenAI:ApiKey"]!;

		/// <summary>
		/// Working models so far
		/// models/BAAI/bge-large-en-v1.5
		/// models/sentence-transformers/all-MiniLM-L6-v2/pipeline/feature-extraction
		/// models/mixedbread-ai/mxbai-embed-large-v1
		/// </summary>
		/// <param name="text"></param>
		/// <returns></returns>
		public async Task<float[]> GetHuggingFaceEmbeddingAsync(string text, string model = "models/BAAI/bge-large-en-v1.5")
		{
			try
			{
				var client = httpFactory.CreateClient("huggingface");
				model = model ?? "models/BAAI/bge-large-en-v1.5";
				var req = new HttpRequestMessage(HttpMethod.Post, model);
				//req.Headers.Add("Authorization", $"Bearer {_hfKey}");
				req.Content = JsonContent.Create(new { inputs = text });
				var res = await client.SendAsync(req);
				if (!res.IsSuccessStatusCode)
				{
					var body = await res.Content.ReadAsStringAsync();
					logger.LogWarning("HuggingFace call failed {Status}: {Body}", res.StatusCode, body);
				}
				res.EnsureSuccessStatusCode();
				var json = await res.Content.ReadFromJsonAsync<JsonElement>();
				//return json[0].GetProperty("embedding")
				//			  .EnumerateArray()
				//			  .Select(x => x.GetSingle())
				//			  .ToArray();
				// Enumerate index [0] directly, as it contains the raw float coordinates array
				return json[0].EnumerateArray()
							  .Select(x => x.GetSingle())
							  .ToArray();
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "HuggingFace CallAsync exception");
				throw;
			}
		}

		/// <summary>
		/// models/gemini-embedding-2
		/// models/gemini-embedding-001
		/// </summary>
		/// <param name="text"></param>
		/// <returns></returns>
		public async Task<float[]> GetGeminiEmbeddingAsync(string text, string model = "models/gemini-embedding-2")
		{
			try
			{
				var client = httpFactory.CreateClient("gemini");
				model = model ?? "models/gemini-embedding-2";
				var req = new HttpRequestMessage(HttpMethod.Post,$"{model}:embedContent?key={_geminiKey}");
				req.Content = JsonContent.Create(new { content = new { parts = new[] { new { text = text } } }, outputDimensionality = 768 });
				var res = await client.SendAsync(req);
				if (!res.IsSuccessStatusCode)
				{
					var body = await res.Content.ReadAsStringAsync();
					logger.LogWarning("GeminiEmbedding call failed {Status}: {Body}", res.StatusCode, body);
				}
				res.EnsureSuccessStatusCode();

				var json = await res.Content.ReadFromJsonAsync<JsonElement>();
				return json.GetProperty("embedding")
						   .GetProperty("values")
						   .EnumerateArray()
						   .Select(x => x.GetSingle())
						   .ToArray();
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "GeminiEmbedding CallAsync exception");
				throw;
			}
		}

		/// <summary>
		/// llama-3.1-8b-instant
		/// llama-3.3-70b-versatile
		/// </summary>
		/// <param name="text"></param>
		/// <returns></returns>
		public async Task<float[]> GetGroqEmbeddingAsync(string text)
		{
			try
			{
				var client = httpFactory.CreateClient("groq");
				var req = new HttpRequestMessage(HttpMethod.Post, "embeddings");
				//req.Headers.Add("Authorization", $"Bearer {_groqKey}");
				req.Content = JsonContent.Create(new
				{
					model = "llama-3.1-8b-instant", // Free on Groq
					input = text
				});
				var res = await client.SendAsync(req);
				if (!res.IsSuccessStatusCode)
				{
					var body = await res.Content.ReadAsStringAsync();
					logger.LogWarning("GroqEmbedding call failed {Status}: {Body}", res.StatusCode, body);
				}
				res.EnsureSuccessStatusCode();

				var json = await res.Content.ReadFromJsonAsync<JsonElement>();
				return json.GetProperty("data")[0]
						   .GetProperty("embedding")
						   .EnumerateArray()
						   .Select(x => x.GetSingle())
						   .ToArray();
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "GroqEmbedding CallAsync exception");
				throw;
			}
		}
		//public async Task<float[]> GetOpenAiEmbeddingAsync(string text)
		//{
		//	try
		//	{
		//		var client = httpFactory.CreateClient("openai");
		//		var req = new HttpRequestMessage(HttpMethod.Post, "embeddings");
		//		req.Headers.Add("Authorization", $"Bearer {_openAiKey}");
		//		req.Headers.Add("Content-Type", "application/json");
		//		req.Content = JsonContent.Create(new
		//		{
		//			model = "text-embedding-3-small",
		//			input = text
		//		});

		//		var res = await client.SendAsync(req);
		//		if (!res.IsSuccessStatusCode)
		//		{
		//			var body = await res.Content.ReadAsStringAsync();
		//			logger.LogWarning("OpenAiEmbedding call failed {Status}: {Body}", res.StatusCode, body);
		//		}
		//		res.EnsureSuccessStatusCode();

		//		var json = await res.Content.ReadFromJsonAsync<JsonElement>();
		//		return json.GetProperty("data")[0]
		//				   .GetProperty("embedding")
		//				   .EnumerateArray()
		//				   .Select(x => x.GetSingle())
		//				   .ToArray();
		//	}
		//	catch (Exception ex)
		//	{
		//		logger.LogError(ex, "OpenAiEmbedding CallAsync exception");
		//		throw;
		//	}
		//}


	}
}
