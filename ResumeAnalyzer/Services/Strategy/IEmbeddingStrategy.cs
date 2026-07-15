namespace ResumeAnalyzer.Services.Strategy
{
	public interface IEmbeddingStrategy
	{
		Task<float[]> GetEmbeddingAsync(string resumeText, CancellationToken ct = default);
		Task<float[]> GetGeminiEmbeddingAsync(string jobDescription, CancellationToken ct = default);
		Task<float[]> GetHuggingFaceEmbeddingAsync(string jobDescription, CancellationToken ct = default);
	}
}
