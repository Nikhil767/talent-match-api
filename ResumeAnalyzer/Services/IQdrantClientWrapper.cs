using Qdrant.Client.Grpc;

namespace ResumeAnalyzer.Services
{
	public interface IQdrantClientWrapper
	{
		Task<IReadOnlyList<string>> ListCollectionsAsync(CancellationToken cancellationToken = default);
		Task CreateCollectionAsync(string collectionName, VectorParams vectorParams, CancellationToken cancellationToken = default);
		Task CreatePayloadIndexAsync(string collectionName, string fieldName, PayloadSchemaType schemaType, bool wait = true, CancellationToken cancellationToken = default);
		Task UpsertAsync(string collectionName, IReadOnlyList<PointStruct> points, CancellationToken cancellationToken = default);
		Task<IReadOnlyList<ScoredPoint>> SearchAsync(string collectionName, ReadOnlyMemory<float> vector, ulong limit, Filter? filter = null, CancellationToken cancellationToken = default);
		Task<ScrollResponse> ScrollAsync(string collectionName, Filter? filter = null, uint limit = 10, WithVectorsSelector? vectorsSelector = null, WithPayloadSelector? payloadSelector = null, CancellationToken cancellationToken = default);
		Task<UpdateResult> DeleteAsync(string collectionName, IReadOnlyList<PointId> points, CancellationToken cancellationToken = default);
		Task<UpdateResult> DeleteAsync(string collectionName, Filter filter, CancellationToken cancellationToken = default);
	}
}
