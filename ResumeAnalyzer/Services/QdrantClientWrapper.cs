using Qdrant.Client;
using Qdrant.Client.Grpc;

namespace ResumeAnalyzer.Services
{
	public class QdrantClientWrapper : IQdrantClientWrapper
	{
		private readonly QdrantClient _client;

		public QdrantClientWrapper(IConfiguration config)
		{
			_client = new QdrantClient(config["Qdrant:Host"]!, https: true, apiKey: config["Qdrant:ApiKey"]!);
		}

		public async Task<IReadOnlyList<string>> ListCollectionsAsync(CancellationToken cancellationToken = default)
			=> await _client.ListCollectionsAsync(cancellationToken);

		public async Task CreateCollectionAsync(string collectionName, VectorParams vectorParams, CancellationToken cancellationToken = default)
			=> await _client.CreateCollectionAsync(collectionName, vectorParams, cancellationToken: cancellationToken);

		public async Task CreatePayloadIndexAsync(string collectionName, string fieldName, PayloadSchemaType schemaType, bool wait = true, CancellationToken cancellationToken = default)
			=> await _client.CreatePayloadIndexAsync(collectionName, fieldName, schemaType, wait: wait, cancellationToken: cancellationToken);

		public async Task UpsertAsync(string collectionName, IReadOnlyList<PointStruct> points, CancellationToken cancellationToken = default)
			=> await _client.UpsertAsync(collectionName, points, cancellationToken: cancellationToken);

		public async Task<IReadOnlyList<ScoredPoint>> SearchAsync(string collectionName, ReadOnlyMemory<float> vector, ulong limit, Filter? filter = null, CancellationToken cancellationToken = default)
			=> await _client.SearchAsync(collectionName, vector, limit: limit, filter: filter, cancellationToken: cancellationToken);

		public async Task<ScrollResponse> ScrollAsync(string collectionName, Filter? filter = null, uint limit = 10, WithVectorsSelector? vectorsSelector = null, WithPayloadSelector? payloadSelector = null, CancellationToken cancellationToken = default)
			=> await _client.ScrollAsync(collectionName, filter: filter, limit: limit, vectorsSelector: vectorsSelector, payloadSelector: payloadSelector, cancellationToken: cancellationToken);

		public async Task<UpdateResult> DeleteAsync(string collectionName, IReadOnlyList<PointId> points, CancellationToken cancellationToken = default)
			=> await _client.DeleteAsync(collectionName, points, cancellationToken: cancellationToken);

		public async Task<UpdateResult> DeleteAsync(string collectionName, Filter filter, CancellationToken cancellationToken = default)
			=> await _client.DeleteAsync(collectionName, filter, cancellationToken: cancellationToken);
	}
}
