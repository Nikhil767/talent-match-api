using Qdrant.Client.Grpc;
using ResumeAnalyzer.Domain.Dto;
using ResumeAnalyzer.Domain.Repositories;
using ResumeAnalyzer.Services.Helper;
using ResumeAnalyzer.Services.Strategy;
using System.Text.Json;

namespace ResumeAnalyzer.Services.Facade
{
	public class JobPipelineService(
		JobIngestionService ingestor,
		IVectorService vectorService,
		IEmbeddingStrategy embeddings,
		IConfiguration config,
		IJobRepository jobRepository,
		IResumeRepository resumeRepository,
		IResumeJobMatchRepository matchRepository,
		IAnalysisStrategy analysis) : IJobPipelineService
	{
		public async Task<object> IngestJobsAsync(JobSearchRequestDto reqDto, CancellationToken ct = default)
		{
			var jobs = await ingestor.FetchJobsByGetAsync(reqDto, ct);
			var ingested = 0;
			if (jobs.IsNotNullOrEmpty())
			{
				var incomingIds = jobs.Select(j => j.JobId);
				var existingJobs = await jobRepository.GetExistingJobIdsAsync(incomingIds, ct);
				var newJobs = existingJobs.IsNotNullOrEmpty() ? jobs.Where(j => !existingJobs.Contains(j.JobId)) : jobs;
				foreach (var job in newJobs)
				{
					job.Id = Guid.NewGuid();
					await jobRepository.AddAsync(job);
					await Task.Delay(TimeSpan.FromSeconds(1), ct);
					var embedding = await embeddings.GetEmbeddingAsync(job.Title + " " + job.Description);
					await vectorService.UpsertAsync(config["Qdrant:QdrantCollections:Jobs"]!, job.Id.ToString(), embedding, new Dictionary<string, object>
					{
						["job_id"] = job.Id,
						["title"] = job.Title!,
						["company"] = job.Company!
					});
					ingested++;
					await Task.Delay(TimeSpan.FromSeconds(2), ct);
				}
				await jobRepository.SaveChangesAsync(ct);
			}
			return new { ingested, reqDto };
		}

		public async Task<object?> MatchJobsToResumeAsync(MatchRequestDto req, Guid userId, CancellationToken ct = default)
		{
			var resume = await resumeRepository.GetResumeAnalysisAsync(x => x.Id == req.ResumeId && x.UserId == userId, ct);
			if (resume is null) return null;

			var filter = new Filter
			{
				Must = { new Condition { Field = new FieldCondition { Key = "resume_id", Match = new Match { Keyword = req.ResumeId.ToString() } } } }
			};
			float[] vectors = null;
			string resumeText = string.Empty;
			var embeddingsList = await vectorService.GetEmbeddingsAsync(config["Qdrant:QdrantCollections:Resumes"]!, filter: filter, ct);
			if (embeddingsList.IsNotNullOrEmpty())
			{
				vectors = embeddingsList.Select(r => r.embeddings).Aggregate((a, b) => [.. a.Zip(b, (x, y) => x + y)]);
				resumeText = string.Concat(embeddingsList.Select(r => r.text));
			}

			var hits = await vectorService.SearchAsync(config["Qdrant:QdrantCollections:Jobs"]!, vectors!, topK: 5, ct: ct);
			var results = new List<object>();
			foreach (var hit in hits)
			{
				Domain.Entities.Job job = null;
				var jobId = hit.Payload["job_id"].StringValue;
				if (Guid.TryParse(jobId, out Guid parsedJobId))
					job = await jobRepository.GetJobsAsync(x => x.Id == parsedJobId, ct);
				if (job is null) continue;

				var existingMatch = await matchRepository.FirstOrDefaultAsync(m => m.ResumeId == resume.ResumeId && m.JobId == job.Id, ct);
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
			return results;
		}
	}
}
