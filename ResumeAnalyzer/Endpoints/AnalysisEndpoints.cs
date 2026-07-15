using Qdrant.Client.Grpc;
using ResumeAnalyzer.Domain.Dto;
using ResumeAnalyzer.Domain.Repositories;
using ResumeAnalyzer.Middleware;
using ResumeAnalyzer.Services;
using ResumeAnalyzer.Services.Helper;
using ResumeAnalyzer.Services.Strategy;

namespace ResumeAnalyzer.Endpoints
{
	public static class AnalysisEndpoints
	{
		public static void MapAnalysisEndpoints(this WebApplication app)
		{
			var group = app.MapGroup("/api/analysis").WithTags("Analysis").RequireAuthorization();

			// POST /analysis/ats
			group.MapPost("/ats", async (AtsRequestDto req, HttpContext ctx, VectorService vectorService,
			IConfiguration config, IResumeAnalysisRepository resumeAnalysisRepository, IAnalysisStrategy analysis,
			IEmbeddingStrategy embedding) =>
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
				var resumeAnalysis = await resumeAnalysisRepository.FirstOrDefaultAsync(x => x.ResumeId == req.ResumeId);
				if (resumeAnalysis is null) return Results.NotFound();

				// 1. Generate an embedding for the target Job Description
				var jobEmbedding = await embedding.GetEmbeddingAsync(jobDescription);
				// 2. Filter Qdrant search strictly to chunks belonging to this specific resume
				var filter = new Filter
				{
					Must = {
						new Condition {
							Field = new FieldCondition {
								Key = "resume_id",
								Match = new Match { Keyword = req.ResumeId.ToString() }
							}
						}
					}
				};
				// 3. Search for the top 5 most relevant chunks to the job description
				var searchResults = await vectorService.SearchAsync(
					config["Qdrant:QdrantCollections:Resumes"]!,
					jobEmbedding,
					topK: 5,
					filter: filter);
				// 4. Extract the raw text from the payload
				var relevantChunks = searchResults.Select(r => r.Payload["chunk_text"].StringValue).ToList();
				// 5. Combine the relevant chunks to form the context for the LLM
				var contextText = string.Join("\n\n... ", relevantChunks);
				// Fallback: If Qdrant returns nothing (e.g. index empty), fallback to the extracted summary
				if (string.IsNullOrWhiteSpace(contextText))
					contextText = resumeAnalysis.ExtractedSummary;
				var result = await analysis.GetAtsScoreAsync(contextText!, jobDescription);
				//if (!string.IsNullOrWhiteSpace(result))
				//{
				//	resumeAnalysis.AtsAnalysisJson = result;
				//	using var doc = System.Text.Json.JsonDocument.Parse(result);
				//	decimal finalAts = 0;
				//	if (doc.RootElement.TryGetProperty("ats_score", out var scoreProp) && scoreProp.TryGetDecimal(out var parsedAts))
				//		finalAts = Math.Clamp(parsedAts, 0, 100);
				//	resumeAnalysis.AtsScore = finalAts > 0 && resumeAnalysis.AtsScore != finalAts ? finalAts : resumeAnalysis.AtsScore;
				//	await resumeAnalysisRepository.AddAsync(resumeAnalysis);
				//	var isSaved = await resumeAnalysisRepository.SaveChangesAsync();
				//}
				return Results.Ok(System.Text.Json.JsonSerializer.Deserialize<object>(result));
			})
			.WithSummary("ATS score + keyword analysis for a resume");

			// POST /analysis/gaps
			group.MapPost("/gaps", async (GapRequestDto req, HttpContext ctx, VectorService vectorService,
			IConfiguration config, IResumeRepository resumeRepository, IAnalysisStrategy analysis, 
			IEmbeddingStrategy embedding) =>
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
				var resume = await resumeRepository.GetResumeAnalysisAsync(x => x.Id == req.ResumeId && x.UserId == userId);
				if (resume is null) return Results.NotFound();
				
				// 1. Generate an embedding for the target Job Description
				var jobEmbedding = await embedding.GetEmbeddingAsync(jobDescription);
				// 2. Filter Qdrant search strictly to chunks belonging to this specific resume
				var filter = new Filter
				{
					Must = {
						new Condition {
							Field = new FieldCondition {
								Key = "resume_id",
								Match = new Match { Keyword = req.ResumeId.ToString() }
							}
						}
					}
				};
				// 3. Search for the top 5 most relevant chunks to the job description
				var searchResults = await vectorService.SearchAsync(
					config["Qdrant:QdrantCollections:Resumes"]!,
					jobEmbedding,
					topK: 5,
					filter: filter);
				// 4. Extract the raw text from the payload
				var relevantChunks = searchResults.Select(r => r.Payload["chunk_text"].StringValue).ToList();

				// 5. Combine the relevant chunks to form the context for the LLM
				var contextText = string.Join("\n\n... ", relevantChunks);
				// Fallback: If Qdrant returns nothing (e.g. index empty), fallback to the extracted summary
				if (string.IsNullOrWhiteSpace(contextText))
					contextText = resume.ExtractedSummary;
				// 6. Pass the highly targeted context text to the LLM
				var result = await analysis.GetGapAnalysisAsync(contextText!, jobDescription);
				return Results.Ok(System.Text.Json.JsonSerializer.Deserialize<object>(result));
			})
			.WithSummary("Skill gap analysis between resume and job description");

			// POST /analysis/tailor
			group.MapPost("/tailor", async (TailorRequestDto req, HttpContext ctx, IResumeRepository resumeRepository, 
			ITailorStrategy tailor, VectorService vectorService, IConfiguration config, IAnalysisStrategy analysis,
			IEmbeddingStrategy embedding) =>
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
				var resume = await resumeRepository.GetResumeAnalysisAsync(x => x.Id == req.ResumeId && x.UserId == userId);
				if (resume is null) return Results.NotFound();

				// 1. Generate an embedding for the target Job Description
				var jobEmbedding = await embedding.GetEmbeddingAsync(jobDescription);
				// 2. Filter Qdrant search strictly to chunks belonging to this specific resume
				var filter = new Filter
				{
					Must = {
						new Condition {
							Field = new FieldCondition {
								Key = "resume_id",
								Match = new Match { Keyword = req.ResumeId.ToString() }
							}
						}
					}
				};
				// 3. Search for the top 5 most relevant chunks to the job description
				var searchResults = await vectorService.SearchAsync(
					config["Qdrant:QdrantCollections:Resumes"]!,
					jobEmbedding,
					topK: 5,
					filter: filter);
				// 4. Extract the raw text from the payload
				var relevantChunks = searchResults.Select(r => r.Payload["chunk_text"].StringValue).ToList();
				// 5. Combine the relevant chunks to form the context for the LLM
				var contextText = string.Join("\n\n... ", relevantChunks);
				// Fallback: If Qdrant returns nothing (e.g. index empty), fallback to the extracted summary
				if (string.IsNullOrWhiteSpace(contextText))
					contextText = resume.ExtractedSummary;
				// 6. Pass the highly targeted context text to the LLM
				var result = await tailor.TailorResumeAsync(contextText!, jobDescription);
				return Results.Ok(System.Text.Json.JsonSerializer.Deserialize<object>(result));
			})
			.WithSummary("Tailor resume bullets to a specific job description");			
		}
	}
}
