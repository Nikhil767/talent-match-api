using Qdrant.Client.Grpc;
using ResumeAnalyzer.Domain.Dto;
using ResumeAnalyzer.Domain.Repositories;
using ResumeAnalyzer.Services.Helper;
using ResumeAnalyzer.Services.Strategy;

namespace ResumeAnalyzer.Services.Facade
{
	public class AnalysisPipelineService(
		IVectorService vectorService,
		IConfiguration config,
		IResumeAnalysisRepository resumeAnalysisRepository,
		IResumeRepository resumeRepository,
		IAnalysisStrategy analysis,
		ITailorStrategy tailor,
		IEmbeddingStrategy embedding) : IAnalysisPipelineService
	{
		public async Task<object?> AnalyzeAtsAsync(AtsRequestDto req, Guid userId)
		{
			var jobDescription = CustomExtensions.SanitizeJobDescription(req.JobDescription);
			var resumeAnalysis = await resumeAnalysisRepository.FirstOrDefaultAsync(x => x.ResumeId == req.ResumeId);
			if (resumeAnalysis is null) return null;

			var jobEmbedding = await embedding.GetEmbeddingAsync(jobDescription);
			var filter = new Filter
			{
				Must = { new Condition { Field = new FieldCondition { Key = "resume_id", Match = new Match { Keyword = req.ResumeId.ToString() } } } }
			};
			var searchResults = await vectorService.SearchAsync(config["Qdrant:QdrantCollections:Resumes"]!, jobEmbedding, topK: 5, filter: filter);
			var relevantChunks = searchResults.Select(r => r.Payload["chunk_text"].StringValue).ToList();
			var contextText = string.Join("\n\n... ", relevantChunks);
			if (string.IsNullOrWhiteSpace(contextText))
				contextText = resumeAnalysis.ExtractedSummary;

			var result = await analysis.GetAtsScoreAsync(contextText!, jobDescription);
			return System.Text.Json.JsonSerializer.Deserialize<object>(result);
		}

		public async Task<object?> AnalyzeGapsAsync(GapRequestDto req, Guid userId)
		{
			var jobDescription = CustomExtensions.SanitizeJobDescription(req.JobDescription);
			var resume = await resumeRepository.GetResumeAnalysisAsync(x => x.Id == req.ResumeId && x.UserId == userId);
			if (resume is null) return null;
			
			var jobEmbedding = await embedding.GetEmbeddingAsync(jobDescription);
			var filter = new Filter
			{
				Must = { new Condition { Field = new FieldCondition { Key = "resume_id", Match = new Match { Keyword = req.ResumeId.ToString() } } } }
			};
			var searchResults = await vectorService.SearchAsync(config["Qdrant:QdrantCollections:Resumes"]!, jobEmbedding, topK: 5, filter: filter);
			var relevantChunks = searchResults.Select(r => r.Payload["chunk_text"].StringValue).ToList();
			var contextText = string.Join("\n\n... ", relevantChunks);
			if (string.IsNullOrWhiteSpace(contextText))
				contextText = resume.ExtractedSummary;

			var result = await analysis.GetGapAnalysisAsync(contextText!, jobDescription);
			return System.Text.Json.JsonSerializer.Deserialize<object>(result);
		}

		public async Task<object?> TailorResumeAsync(TailorRequestDto req, Guid userId)
		{
			var jobDescription = CustomExtensions.SanitizeJobDescription(req.JobDescription);
			var resume = await resumeRepository.GetResumeAnalysisAsync(x => x.Id == req.ResumeId && x.UserId == userId);
			if (resume is null) return null;

			var jobEmbedding = await embedding.GetEmbeddingAsync(jobDescription);
			var filter = new Filter
			{
				Must = { new Condition { Field = new FieldCondition { Key = "resume_id", Match = new Match { Keyword = req.ResumeId.ToString() } } } }
			};
			var searchResults = await vectorService.SearchAsync(config["Qdrant:QdrantCollections:Resumes"]!, jobEmbedding, topK: 5, filter: filter);
			var relevantChunks = searchResults.Select(r => r.Payload["chunk_text"].StringValue).ToList();
			var contextText = string.Join("\n\n... ", relevantChunks);
			if (string.IsNullOrWhiteSpace(contextText))
				contextText = resume.ExtractedSummary;

			var result = await tailor.TailorResumeAsync(contextText!, jobDescription);
			return System.Text.Json.JsonSerializer.Deserialize<object>(result);
		}
	}
}
