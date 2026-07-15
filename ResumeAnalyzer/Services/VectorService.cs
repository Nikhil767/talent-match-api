using Qdrant.Client;
using Qdrant.Client.Grpc;
using System.Text.Json;

namespace ResumeAnalyzer.Services
{
	public class VectorService(IConfiguration config)
	{
		private readonly ulong _vectorSize = ulong.TryParse(config["Qdrant:VectorSize"], out var size) ? size : 768;
		private readonly QdrantClient _client = new(
			config["Qdrant:Host"]!, https: true,
			apiKey: config["Qdrant:ApiKey"]!);

		public async Task EnsureCollectionAsync(string name)
		{
			var collections = await _client.ListCollectionsAsync();
			if (!collections.Any(c => c == name))
			{
				await _client.CreateCollectionAsync(name,
					new VectorParams { Size = _vectorSize, Distance = Distance.Cosine });
			}
			// 3. IMMEDIATELY call your index creation method right here!
			await _client.CreatePayloadIndexAsync(
				collectionName: name,
				fieldName: "resume_id",
				schemaType : PayloadSchemaType.Keyword,
				//indexParams: , // Matches your metadata type
				wait: true
			);
		}

		public async Task UpsertAsync(string collection, string id, float[] vector, object payload, CancellationToken ct = default)
		{
			var payloadDict = JsonSerializer.Deserialize<Dictionary<string, object>>(
					JsonSerializer.Serialize(payload)
				)!
				.ToDictionary(
					kv => kv.Key,
					kv => kv.Value.ToQdrantValue()
				);

			await _client.UpsertAsync(collection, new[] {
			new PointStruct {
				Id   = new PointId { Uuid = id },
				Vectors = new Vectors { Vector = new Qdrant.Client.Grpc.Vector { Data = { vector } } },
				Payload = { payloadDict }
			}}, cancellationToken: ct);
		}

		public async Task<List<ScoredPoint>> SearchAsync(string collection, float[] query, int topK = 10, Filter? filter = null, CancellationToken ct = default)
		{
			var result = await _client.SearchAsync(
				collectionName: collection,
				vector: query,
				limit: (ulong)topK,
				filter: filter,
				cancellationToken: ct);
			return [.. result];
		}

		public async Task<List<(string text, float[] embeddings)>> GetEmbeddingsAsync(string collection,
		Filter? filter = null,
		CancellationToken ct = default)
		{
			// Use ScrollAsync instead of SearchAsync when you don't have a query vector
			var result = await _client.ScrollAsync(
				collectionName: collection,
				filter: filter,
				limit: 100, // Replace ulong.MaxValue with a realistic batch limit to protect memory
				vectorsSelector: new WithVectorsSelector { Enable = true },
				payloadSelector: new WithPayloadSelector { Enable = true }, // Set to true to read "chunk_text"
				cancellationToken: ct
			);
			var list = new List<(string text, float[] embeddings)>();
			foreach (var point in result.Result)
			{
				// 1. Safely extract the vector data
				float[]? vectorData = point.Vectors?.Vector?.Dense?.Data?.ToArray();//point.Vectors?.Vector?.Data?.ToArray();
				// 2. Safely extract the text payload
				string textChunk = string.Empty;
				if (point.Payload != null && point.Payload.TryGetValue("chunk_text", out var value))				
					textChunk = value.StringValue;				
				if (vectorData != null)				
					list.Add((textChunk, vectorData));				
			}
			return list;
		}


		[Obsolete("Use DeleteByResumeIdAsync")]
		public async Task DeleteAsync(string collection, string id, CancellationToken ct = default)
		{
			await _client.DeleteAsync(collection, new[] { new PointId { Uuid = id } }, cancellationToken: ct);
		}

		public async Task<bool> DeleteByResumeIdAsync(string collection, string resumeId, CancellationToken ct = default)
		{
			Filter filter = new()
			{
				Must =
				{
					new Condition
					{
						Field = new FieldCondition
						{
							Key = "resume_id",
							Match = new Match { Keyword = resumeId }
						}
					}
				}
			};
			var result = await _client.DeleteAsync(collection, filter, cancellationToken: ct);
			return result.Status == UpdateStatus.Completed;
		}
	}

	// Helper extension to convert C# objects → Qdrant gRPC Value
	public static class QdrantValueExtensions
	{
		public static Value ToQdrantValue(this object value)
		{
			return value switch
			{
				null => new Value { NullValue = Qdrant.Client.Grpc.NullValue.NullValue },
				string s => new Value { StringValue = s },
				int i => new Value { IntegerValue = i },
				long l => new Value { IntegerValue = l },
				float f => new Value { DoubleValue = f },
				double d => new Value { DoubleValue = d },
				bool b => new Value { BoolValue = b },
				IEnumerable<object> list => new Value
				{
					ListValue = new ListValue
					{
						Values = { list.Select(ToQdrantValue) }
					}
				},
				_ => new Value { StringValue = value.ToString()! }
			};
		}
	}
}