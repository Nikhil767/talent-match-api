using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ResumeAnalyzer.Domain.Entities
{
	[Table("resume_job_matches")]
	public class ResumeJobMatch
	{
		[Key]
		[Column("id")]
		public Guid Id { get; set; } = Guid.NewGuid();

		[Required]
		[Column("resume_id")]
		public Guid ResumeId { get; set; }

		[Required]
		[Column("job_id")]
		public Guid JobId { get; set; }

		[Required]
		[Column("match_score")]
		public decimal MatchScore { get; set; } = 0;

		[Required]
		[Column("matched_skills_json", TypeName = "jsonb")]
		public string MatchedSkillsJson { get; set; } = "[]";

		[Required]
		[Column("missing_skills_json", TypeName = "jsonb")]
		public string MissingSkillsJson { get; set; } = "[]";

		[Column("model_used")]
		public string? ModelUsed { get; set; }

		[Column("matched_at")]
		public DateTime? MatchedAt { get; set; }

		[Column("created_at")]
		public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

		[Column("updated_at")]
		public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

		[ForeignKey(nameof(ResumeId))]
		public Resume? Resume { get; set; }

		[ForeignKey(nameof(JobId))]
		public Job? Job { get; set; }
	}
}
