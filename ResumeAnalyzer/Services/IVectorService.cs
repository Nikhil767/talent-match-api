using Qdrant.Client.Grpc;

namespace ResumeAnalyzer.Services
{
	public interface IVectorService
	{
		Task EnsureCollectionAsync(string name);
		Task UpsertAsync(string collection, string id, float[] vector, object payload, CancellationToken ct = default);
		Task<List<ScoredPoint>> SearchAsync(string collection, float[] query, int topK = 10, Filter? filter = null, CancellationToken ct = default);
		Task<List<(string text, float[] embeddings)>> GetEmbeddingsAsync(string collection, Filter? filter = null, CancellationToken ct = default);
		Task DeleteAsync(string collection, string id, CancellationToken ct = default);
		Task<bool> DeleteByResumeIdAsync(string collection, string resumeId, CancellationToken ct = default);
	}
}
