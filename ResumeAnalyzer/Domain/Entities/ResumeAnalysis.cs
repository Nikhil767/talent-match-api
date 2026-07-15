using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ResumeAnalyzer.Domain.Entities
{
	[Table("resume_analysis")]
	public class ResumeAnalysis
	{
		[Key]
		[Column("id")]
		public Guid Id { get; set; } = Guid.NewGuid();

		[Required]
		[Column("resume_id")]
		public Guid ResumeId { get; set; }

		[Required]
		[Column("ats_score")]
		public decimal AtsScore { get; set; } = 0;

		[Required]
		[Column("ats_analysis_json", TypeName = "jsonb")]
		public string AtsAnalysisJson { get; set; } = "[]";

		[Required]
		[Column("skills_json", TypeName = "jsonb")]
		public string SkillsJson { get; set; } = "[]";

		[Column("extracted_summary")]
		public string? ExtractedSummary { get; set; }

		[Column("extracted_text_preview")]
		public string? ExtractedTextPreview { get; set; }

		[Column("model_used")]
		public string? ModelUsed { get; set; }

		[Column("analyzed_at")]
		public DateTime? AnalyzedAt { get; set; }

		[Column("created_at")]
		public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

		[Column("updated_at")]
		public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

		[ForeignKey(nameof(ResumeId))]
		public Resume? Resume { get; set; }
	}
}
