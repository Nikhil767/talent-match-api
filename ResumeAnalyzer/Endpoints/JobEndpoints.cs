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
			// POST /jobs/ingest?query=software engineer&location=remote
			group.MapPost("/ingest", async (
				JobSearchRequestDto reqDto,
				JobIngestionService ingestor,
				VectorService vector,
				IEmbeddingStrategy embeddings,
				IConfiguration config,
				IJobRepository jobRepository,
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
				var jobs = await ingestor.FetchJobsByGetAsync(reqDto, ct);
				var ingested = 0;
				if (jobs.IsNotNullOrEmpty())
				{
					// before adding to db check for the job_id exists in db then dont add
					var incomingIds = jobs.Select(j => j.JobId);
					var existingJobs = await jobRepository.GetExistingJobIdsAsync(incomingIds, ct);
					var newJobs = existingJobs.IsNotNullOrEmpty() ? jobs.Where(j => !existingJobs.Contains(j.JobId)) : jobs;
					foreach (var job in newJobs)
					{
						job.Id = Guid.NewGuid();
						await jobRepository.AddAsync(job);
						await Task.Delay(TimeSpan.FromSeconds(1), ct);
						var embedding = await embeddings.GetEmbeddingAsync(job.Title + " " + job.Description);
						await vector.UpsertAsync(config["Qdrant:QdrantCollections:Jobs"]!, job.Id.ToString(), embedding, new Dictionary<string, object>
						{
							["job_id"] = job.Id,
							["title"] = job.Title!,
							["company"] = job.Company!
						});
						ingested++;
						await Task.Delay(TimeSpan.FromSeconds(2), ct);
					}
					var isSaved = await jobRepository.SaveChangesAsync(ct);
				}
				return Results.Ok(new { ingested, reqDto });
			})
			.WithSummary("Fetch jobs from JSearch and store in Qdrant + Supabase");

			// POST /jobs/match  { resumeId }
			group.MapPost("/match", async (
				MatchRequestDto req,
				HttpContext ctx,
				IResumeRepository resumeRepository,
				IJobRepository jobRepository,
				IResumeJobMatchRepository matchRepository,
				IEmbeddingStrategy embeddings,
				IAnalysisStrategy analysis,
				IConfiguration config,
				VectorService vectorService,
				CancellationToken ct = default) =>
			{
				if (req.ResumeId == Guid.Empty)
					return Results.BadRequest("Invalid resumeId");
				var userId = ctx.User.GetGuidUserId();
				var resume = await resumeRepository.GetResumeAnalysisAsync(x => x.Id == req.ResumeId && x.UserId == userId, ct);
				if (resume is null) return Results.NotFound();

				// 1. Embed resume
				//var embedding = await embeddings.GetEmbeddingAsync(resume.ExtractedSummary);
				var filter = new Filter
				{
					Must = { new Condition { Field = new FieldCondition { Key = "resume_id", Match = new Match { Keyword = req.ResumeId.ToString()}}}}
				};
				float[] vectors = null;
				string resumeText = string.Empty;
				var embeddingsList = await vectorService.GetEmbeddingsAsync(config["Qdrant:QdrantCollections:Resumes"]!, filter: filter, ct);
				if (embeddingsList.IsNotNullOrEmpty())
				{
					vectors = embeddingsList.Select(r => r.embeddings).Aggregate((a, b) => [.. a.Zip(b, (x, y) => x + y)]);
					resumeText = string.Concat(embeddingsList.Select(r => r.text));
				}
				// 2. Vector search in jobs collection
				var hits = await vectorService.SearchAsync(config["Qdrant:QdrantCollections:Jobs"]!, vectors!, topK: 5, ct: ct);
				// 3. For each hit, fetch job from DB and get LLM explanation
				var results = new List<object>();
				foreach (var hit in hits)
				{
					Domain.Entities.Job job = null;
					var jobId = hit.Payload["job_id"].StringValue;
					if (Guid.TryParse(jobId, out Guid parsedJobId))
						job = await jobRepository.GetJobsAsync(x => x.Id == parsedJobId, ct);					
					if (job is null) continue;
					// Check if a match already exists
					var existingMatch = await matchRepository.FirstOrDefaultAsync(m => m.ResumeId == resume.Id && m.JobId == job.Id, ct);
					if (existingMatch != null)
					{
						results.Add(new
						{
							job,
							vector_score = existingMatch.MatchScore,
							ai_analysis = JsonSerializer.Deserialize<object>(existingMatch.MatchedSkillsJson)
						});
						continue;
					}
					await Task.Delay(TimeSpan.FromMilliseconds(100), ct);
					var explanation = await analysis.ExplainMatchAsync(resumeText, job.Title + "\n" + job.Description);
					var newMatch = new ResumeAnalyzer.Domain.Entities.ResumeJobMatch
					{
						ResumeId = resume.ResumeId,
						JobId = job.Id,
						MatchScore = (decimal)hit.Score,
						MatchedSkillsJson = explanation,
						MatchedAt = DateTime.UtcNow
					};
					await matchRepository.AddAsync(newMatch, ct);
					results.Add(new
					{
						job,
						vector_score = hit.Score,
						ai_analysis = JsonSerializer.Deserialize<object>(explanation)
					});
				}
				await matchRepository.SaveChangesAsync(ct);
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
