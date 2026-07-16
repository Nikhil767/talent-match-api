using ResumeAnalyzer.Domain.Dto;
using ResumeAnalyzer.Middleware;
using ResumeAnalyzer.Services.Helper;

namespace ResumeAnalyzer.Endpoints
{
	public static class AnalysisEndpoints
	{
		public static void MapAnalysisEndpoints(this WebApplication app)
		{
			var group = app.MapGroup("/api/analysis").WithTags("Analysis").RequireAuthorization();

			// POST /analysis/ats
			group.MapPost("/ats", async (AtsRequestDto req, HttpContext ctx, IConfiguration config, ResumeAnalyzer.Services.Facade.IAnalysisPipelineService pipeline) =>
			{
				if (req.ResumeId == Guid.Empty)
					return Results.BadRequest("Invalid id");
				if (string.IsNullOrWhiteSpace(req.JobDescription))
					return Results.BadRequest("JobDescription cannot be empty.");
				var minLength = int.Parse(config["JobDescription:MinLength"]!);
				var maxLength = int.Parse(config["JobDescription:MaxLength"]!);
				if (req.JobDescription.Length < minLength)
					return Results.BadRequest($"JobDescription must be at least {minLength} characters.");
				if (req.JobDescription.Length > maxLength)
					return Results.BadRequest($"JobDescription cannot be more than {maxLength} characters.");
				var jobDescription = CustomExtensions.SanitizeJobDescription(req.JobDescription);
				if (string.IsNullOrWhiteSpace(jobDescription))
					return Results.BadRequest("Invalid JobDescription");
				var userId = ctx.User.GetGuidUserId();
				var result = await pipeline.AnalyzeAtsAsync(req, userId);
				if (result is null) return Results.NotFound();
				
				return Results.Ok(result);
			})
			.WithSummary("ATS score + keyword analysis for a resume");

			// POST /analysis/gaps
			group.MapPost("/gaps", async (GapRequestDto req, HttpContext ctx, IConfiguration config, ResumeAnalyzer.Services.Facade.IAnalysisPipelineService pipeline) =>
			{
				if (req.ResumeId == Guid.Empty)
					return Results.BadRequest("Invalid id");
				if (string.IsNullOrWhiteSpace(req.JobDescription))
					return Results.BadRequest("JobDescription cannot be empty.");
				var minLength = int.Parse(config["JobDescription:MinLength"]!);
				var maxLength = int.Parse(config["JobDescription:MaxLength"]!);
				if (req.JobDescription.Length < minLength)
					return Results.BadRequest($"JobDescription must be at least {minLength} characters.");
				if (req.JobDescription.Length > maxLength)
					return Results.BadRequest($"JobDescription cannot be more than {maxLength} characters.");
				var jobDescription = CustomExtensions.SanitizeJobDescription(req.JobDescription);
				if (string.IsNullOrWhiteSpace(jobDescription))
					return Results.BadRequest("Invalid JobDescription");
				var userId = ctx.User.GetGuidUserId();
				var result = await pipeline.AnalyzeGapsAsync(req, userId);
				if (result is null) return Results.NotFound();
				
				return Results.Ok(result);
			})
			.WithSummary("Skill gap analysis between resume and job description");

			// POST /analysis/tailor
			group.MapPost("/tailor", async (TailorRequestDto req, HttpContext ctx, IConfiguration config, ResumeAnalyzer.Services.Facade.IAnalysisPipelineService pipeline) =>
			{
				if (req.ResumeId == Guid.Empty)
					return Results.BadRequest("Invalid id");
				if (string.IsNullOrWhiteSpace(req.JobDescription))
					return Results.BadRequest("JobDescription cannot be empty.");
				var minLength = int.Parse(config["JobDescription:MinLength"]!);
				var maxLength = int.Parse(config["JobDescription:MaxLength"]!);
				if (req.JobDescription.Length < minLength)
					return Results.BadRequest($"JobDescription must be at least {minLength} characters.");
				if (req.JobDescription.Length > maxLength)
					return Results.BadRequest($"JobDescription cannot be more than {maxLength} characters.");
				var jobDescription = CustomExtensions.SanitizeJobDescription(req.JobDescription);
				if (string.IsNullOrWhiteSpace(jobDescription))
					return Results.BadRequest("Invalid JobDescription");
				var userId = ctx.User.GetGuidUserId();
				var result = await pipeline.TailorResumeAsync(req, userId);
				if (result is null) return Results.NotFound();
				
				return Results.Ok(result);
			})
			.WithSummary("Tailor resume bullets to a specific job description");			
		}
	}
}
