using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ResumeAnalyzer.Domain.Entities
{
	[Table("resumes")]
	public class Resume
	{
		[Key]
		[Column("id")]
		public Guid Id { get; set; } = Guid.NewGuid();

		[Required]
		[Column("user_id")]
		public Guid UserId { get; set; }

		[Required]
		[Column("file_name")]
		public string FileName { get; set; } = string.Empty;

		[Required]
		[Column("file_hash")]
		[StringLength(64)]
		public string FileHash { get; set; } = string.Empty;

		[Required]
		[Column("storage_path")]
		public string StoragePath { get; set; } = string.Empty;

		[Column("mime_type")]
		public string? MimeType { get; set; }

		[Column("file_size_bytes")]
		public long? FileSizeBytes { get; set; }

		[Required]
		[Column("status")]
		public string Status { get; set; } = "queued";

		[Column("processing_step")]
		public string? ProcessingStep { get; set; }

		[Required]
		[Column("attempt_count")]
		public int AttemptCount { get; set; } = 0;

		[Column("locked_at")]
		public DateTime? LockedAt { get; set; }

		[Column("processed_at")]
		public DateTime? ProcessedAt { get; set; }

		[Column("error_message")]
		public string? ErrorMessage { get; set; }

		[Column("created_at")]
		public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

		[Column("updated_at")]
		public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
	}
}
