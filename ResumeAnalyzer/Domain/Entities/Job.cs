using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ResumeAnalyzer.Domain.Entities
{
	[Table("jobs")]
	public class Job
	{
		[Key]
		[Column("id")]
		public Guid Id { get; set; } = Guid.NewGuid();

		[Column("job_id")]
		public string JobId { get; set; }

		[Column("title")]
		public string? Title { get; set; }

		[Column("company")]
		public string? Company { get; set; }

		[Column("location")]
		public string? Location { get; set; }

		[Column("description")]
		public string? Description { get; set; }

		[Column("url")]
		public string? Url { get; set; }

		[Column("posted_at")]
		public string? PostedAt { get; set; }

		[Column("source")]
		public string? Source { get; set; }

		[Column("created_at")]
		public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

		[Column("updated_at")]
		public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
	}
}
