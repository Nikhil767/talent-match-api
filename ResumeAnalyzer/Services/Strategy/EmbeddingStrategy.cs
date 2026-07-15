using ResumeAnalyzer.Services.Providers;

namespace ResumeAnalyzer.Services.Strategy
{
	public class EmbeddingStrategy(EmbeddingService embed) : IEmbeddingStrategy
	{
		public async Task<float[]> GetEmbeddingAsync(string jobDescription, CancellationToken ct = default)
		{
			var providers = new List<Func<CancellationToken, Task<float[]>>>
			{
				token => embed.GetGeminiEmbeddingAsync(jobDescription, "models/gemini-embedding-2"),
				token => embed.GetGeminiEmbeddingAsync(jobDescription, "models/gemini-embedding-001"),
				token => embed.GetHuggingFaceEmbeddingAsync(jobDescription, "models/BAAI/bge-large-en-v1.5"),
				token => embed.GetHuggingFaceEmbeddingAsync(jobDescription, "models/mixedbread-ai/mxbai-embed-large-v1")				
			};
			foreach (var provider in providers)
			{
				try
				{
					return await provider(ct);
				}
				catch {}
			}
			throw new Exception("All embedding providers failed.");
		}

		/// <summary>
		/// For 8000 character length text
		/// </summary>
		/// <param name="jobDescription"></param>
		/// <param name="ct"></param>
		/// <returns></returns>
		/// <exception cref="Exception"></exception>
		public async Task<float[]> GetGeminiEmbeddingAsync(string jobDescription, CancellationToken ct = default)
		{
			var providers = new List<Func<CancellationToken, Task<float[]>>>
			{
				token => embed.GetGeminiEmbeddingAsync(jobDescription, "models/gemini-embedding-2"),
				token => embed.GetGeminiEmbeddingAsync(jobDescription, "models/gemini-embedding-001")
			};
			foreach (var provider in providers)
			{
				try
				{
					return await provider(ct);
				}
				catch { }
			}
			throw new Exception("All embedding providers failed.");
		}

		/// <summary>
		/// For 2000 character length text
		/// </summary>
		/// <param name="jobDescription"></param>
		/// <param name="ct"></param>
		/// <returns></returns>
		/// <exception cref="Exception"></exception>
		public async Task<float[]> GetHuggingFaceEmbeddingAsync(string jobDescription, CancellationToken ct = default)
		{
			var providers = new List<Func<CancellationToken, Task<float[]>>>
			{
				token => embed.GetHuggingFaceEmbeddingAsync(jobDescription, "models/BAAI/bge-large-en-v1.5"),
				token => embed.GetHuggingFaceEmbeddingAsync(jobDescription, "models/mixedbread-ai/mxbai-embed-large-v1")
			};
			foreach (var provider in providers)
			{
				try
				{
					return await provider(ct);
				}
				catch { }
			}
			throw new Exception("All embedding providers failed.");
		}
	}
}
