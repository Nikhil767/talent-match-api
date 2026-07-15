using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ResumeAnalyzer.Domain.Entities
{
	[Table("resume_chunks")]
	public class ResumeChunk
	{
		[Key]
		[Column("id")]
		public Guid Id { get; set; } = Guid.NewGuid();

		[Required]
		[Column("resume_id")]
		public Guid ResumeId { get; set; }

		[Required]
		[Column("chunk_index")]
		public int ChunkIndex { get; set; }

		[Required]
		[Column("chunk_text")]
		public string ChunkText { get; set; } = string.Empty;

		[Column("qdrant_point_id")]
		public string? QdrantPointId { get; set; }

		[Column("token_count")]
		public int? TokenCount { get; set; }

		[Column("created_at")]
		public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

		[ForeignKey(nameof(ResumeId))]
		public Resume? Resume { get; set; }
	}
}
