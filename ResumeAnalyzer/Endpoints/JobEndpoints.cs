using Qdrant.Client.Grpc;
using ResumeAnalyzer.Domain.Constants;
using ResumeAnalyzer.Domain.Dto;
using ResumeAnalyzer.Domain.Repositories;
using ResumeAnalyzer.Middleware;
using ResumeAnalyzer.Services;
using ResumeAnalyzer.Services.Helper;
using ResumeAnalyzer.Services.Strategy;
using System.Text.Json;
using Filter = Qdrant.Client.Grpc.Filter;

namespace ResumeAnalyzer.Endpoints
{
	public static class JobEndpoints
	{
		public static void MapJobEndpoints(this WebApplication app)
		{
			var group = app.MapGroup("/api/jobs").WithTags("Jobs").RequireAuthorization();
			group.MapPost("/ingest", async (
				JobSearchRequestDto reqDto,
				ResumeAnalyzer.Services.Facade.IJobPipelineService pipeline,
				CancellationToken ct = default) =>
			{
				if (string.IsNullOrWhiteSpace(reqDto.Query))
					return Results.BadRequest("Invalid query");
				if (string.IsNullOrWhiteSpace(reqDto.Location))
					return Results.BadRequest("Invalid location");
				if (string.IsNullOrWhiteSpace(reqDto.Country))
					return Results.BadRequest("Invalid Country");
				if (reqDto.Query.Length > 35)
					return Results.BadRequest("Query strings must be under 35 characters.");
				if (reqDto.Location.Length > 35)
					return Results.BadRequest("Location strings must be under 35 characters.");
				if (reqDto.Country.Length > 90)
					return Results.BadRequest("Country strings must be under 90 characters.");
				if (reqDto.Page < 1)
					return Results.BadRequest("Page indexing must start at 1.");
				if (!string.IsNullOrWhiteSpace(reqDto.EmploymentType) && !CustomConstant.EMPLOYEETYPES.Contains(reqDto.EmploymentType.ToUpperInvariant()))
					return Results.BadRequest(new { Error = "Invalid EmploymentType", AllowedValues = CustomConstant.EMPLOYEETYPES });
				if (!string.IsNullOrWhiteSpace(reqDto.DatePosted) && !CustomConstant.DATETYPES.Contains(reqDto.DatePosted.ToLowerInvariant()))
					return Results.BadRequest(new { Error = "Invalid DatePosted", AllowedValues = CustomConstant.DATETYPES });
				var result = await pipeline.IngestJobsAsync(reqDto, ct);
				return Results.Ok(result);
			})
			.WithSummary("Fetch jobs from JSearch and store in Qdrant + Supabase");

			group.MapPost("/match", async (
				MatchRequestDto req,
				HttpContext ctx,
				ResumeAnalyzer.Services.Facade.IJobPipelineService pipeline,
				CancellationToken ct = default) =>
			{
				if (req.ResumeId == Guid.Empty)
					return Results.BadRequest("Invalid resumeId");
				var userId = ctx.User.GetGuidUserId();
				var results = await pipeline.MatchJobsToResumeAsync(req, userId, ct);
				if (results is null) return Results.NotFound();
				
				return Results.Ok(results);
			})
			.WithSummary("Semantic job matching for a resume");

			// GET /jobs/search?q=keyword
			group.MapGet("/search", async (string q, IJobRepository jobRepository, CancellationToken ct = default) =>
			{
				if (string.IsNullOrWhiteSpace(q))
					return Results.BadRequest("Invalid input");
				if (q.Length > 100 || q.Length > 100)
					return Results.BadRequest("Query strings must be under 100 characters.");
				var jobs = await jobRepository.SearchJobsAsync(q, ct);
				return Results.Ok(jobs);
			})
			.WithSummary("Keyword search across stored jobs");

			// GET /jobs/{id}
			group.MapGet("/{id}", async (Guid id, IJobRepository jobRepository, CancellationToken ct = default) =>
			{
				if (id == Guid.Empty)
					return Results.BadRequest("Invalid id");
				var job = await jobRepository.GetByIdAsync(id);
				return job is null ? Results.NotFound() : Results.Ok(job);
			})
			.WithSummary("Get a single job by ID");
		}
	}
}
