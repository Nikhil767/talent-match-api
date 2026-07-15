using ResumeAnalyzer.Domain.Constants;
using ResumeAnalyzer.Domain.Entities;
using ResumeAnalyzer.Domain.Repositories;
using ResumeAnalyzer.Services.Helper;
using ResumeAnalyzer.Services.Sse;
using ResumeAnalyzer.Services.Strategy;

namespace ResumeAnalyzer.Services.Facade
{
	public class ResumePipelineService(
		VectorService vectorService,
		SupabaseStorageRestService supabaseStorageService,
		IResumeRepository resumeRepository,
		IResumeAnalysisRepository analysisRepository,
		IEmbeddingStrategy embedding,
		IAnalysisStrategy analysis,
		IConfiguration config,
		ISseBroker sseBroker,
		ILogger<ResumePipelineService> logger)
	{
		public async Task<bool> ProcessAsync(Guid recordId, string bearerToken, CancellationToken stoppingToken)
		{
			bool result = false;
			var existingResume = await resumeRepository.GetByIdAsync(recordId);
			if (existingResume is null) return false;
			try
			{
				var file = await supabaseStorageService.DownloadAsync(config["Supabase:StorageBucket"]!, existingResume.StoragePath, bearerToken);
				sseBroker.Publish(existingResume.UserId, CustomConstant.ResumeProgress, "Extracting Text from Resume",
				ProcessingStepValues.Extracted, data: new { ResumeId = existingResume.Id });
				var fileText = file.ExtractText(Path.GetExtension(existingResume.FileName));
				sseBroker.Publish(existingResume.UserId, CustomConstant.ResumeProgress, "Extracted Text from Resume",
				ProcessingStepValues.Extracted, data: new { ResumeId = existingResume.Id });
				result = await InternalProcessAsync(existingResume, fileText, stoppingToken);

				existingResume.Status = result ? ResumeStatusValues.Completed : ResumeStatusValues.Failed;
				existingResume.ProcessingStep = result ? ProcessingStepValues.Done : ProcessingStepValues.Failed;
				existingResume.ProcessedAt = DateTime.UtcNow;
				sseBroker.Publish(existingResume.UserId, CustomConstant.ResumeProgress, $"Resume Parsing {ProcessingStepValues.Done}",
				existingResume.ProcessingStep, data: new { ResumeId = existingResume.Id });
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "Resume processing failed for {Id}", recordId);
				existingResume.Status = ResumeStatusValues.Failed;
				existingResume.ProcessingStep = ProcessingStepValues.Failed;
				existingResume.ErrorMessage = ex.Message;
				result = false;
				sseBroker.Publish(existingResume.UserId, CustomConstant.ResumeProgress, $"Resume Parsing {ProcessingStepValues.Failed}",
				existingResume.ProcessingStep, data: new { ResumeId = existingResume.Id });
			}
			existingResume.LockedAt = null;
			resumeRepository.Update(existingResume);
			await resumeRepository.SaveChangesAsync();
			await Task.Delay(500, stoppingToken);
			sseBroker.Disconnect(existingResume.UserId);
			return result;
		}

		public async Task<bool> InternalProcessAsync(Resume existingResume, string resumeText, CancellationToken stoppingToken)
		{
			sseBroker.Publish(existingResume.UserId, CustomConstant.ResumeProgress, $"Sanitizing Resume", ProcessingStepValues.Sanitize, data: new { ResumeId = existingResume.Id });
			resumeText = CustomExtensions.CleanBeforeChunking(resumeText);
			sseBroker.Publish(existingResume.UserId, CustomConstant.ResumeProgress, $"Resume Sanitization done", ProcessingStepValues.Sanitize, data: new { ResumeId = existingResume.Id });
			// Step 1: Try to embed the ENTIRE resume at once using Gemini
			float[]? completeEmbedding = null;
			bool skippedChunking = false;
			try
			{
				sseBroker.Publish(existingResume.UserId, CustomConstant.ResumeProgress, $"Resume Embedding", ProcessingStepValues.Embedded, data: new { ResumeId = existingResume.Id });
				// Explicitly target Gemini directly here, bypassing the HF fallback models initially
				completeEmbedding = await embedding.GetGeminiEmbeddingAsync(resumeText);
				sseBroker.Publish(existingResume.UserId, CustomConstant.ResumeProgress, $"Resume Embedded", ProcessingStepValues.Embedded, data: new { ResumeId = existingResume.Id });
				skippedChunking = true;
			}
			catch (Exception ex)
			{
				sseBroker.Publish(existingResume.UserId, CustomConstant.ResumeProgress, $"Resume Embedding Failed", ProcessingStepValues.Embedded, data: new { ResumeId = existingResume.Id });
				logger.LogError(ex, "InternalProcessAsync : Primary un-chunked Gemini path failed");
			}
			// Step 2: Ingest into Qdrant based on the layout state
			string qdrantCollection = config["Qdrant:QdrantCollections:Resumes"]!;
			if (skippedChunking && completeEmbedding?.Length > 0)
			{
				// Save the entire resume as a single clean vector point
				await vectorService.UpsertAsync(qdrantCollection, existingResume.Id.ToString(), completeEmbedding,
					BuildMetadata(existingResume, 0, resumeText));
			}
			else
			{   // Fallback: Breakdown into 2000-character segments for Hugging Face models
				sseBroker.Publish(existingResume.UserId, CustomConstant.ResumeProgress, $"Resume Chunking", ProcessingStepValues.Chunked, data: new { ResumeId = existingResume.Id });
				var chunks = CustomExtensions.ChunkByParagraph(resumeText, 2000);
				for (int i = 0; i < chunks.Count; i++)
				{
					var chunkText = chunks[i];
					var chunkEmbedding = await embedding.GetHuggingFaceEmbeddingAsync(chunkText);
					if (chunkEmbedding?.Length > 0)
					{
						await vectorService.UpsertAsync(qdrantCollection, Guid.NewGuid().ToString(), chunkEmbedding,
							BuildMetadata(existingResume, i, chunkText));
					}
				}
				sseBroker.Publish(existingResume.UserId, CustomConstant.ResumeProgress, $"Resume Chunked", ProcessingStepValues.Chunked, data: new { ResumeId = existingResume.Id });
			}
			decimal finalAts = 0;
			sseBroker.Publish(existingResume.UserId, CustomConstant.ResumeProgress, $"Resume Scoring", ProcessingStepValues.Scored, data: new { ResumeId = existingResume.Id });
			var skillsStr = await analysis.ExtractSkillsAsync(resumeText);
			sseBroker.Publish(existingResume.UserId, CustomConstant.ResumeProgress, $"Resume Scored", ProcessingStepValues.Scored, data: new { ResumeId = existingResume.Id });
			string finalSkillsJson = "";
			string extractedSummary = string.Empty;
			if (!string.IsNullOrWhiteSpace(skillsStr))
			{
				try
				{
					using var doc = System.Text.Json.JsonDocument.Parse(skillsStr);
					if (doc.RootElement.TryGetProperty("ats_score", out var scoreProp) && scoreProp.TryGetDecimal(out var parsedAts))
						finalAts = Math.Clamp(parsedAts, 0, 100);
					if (doc.RootElement.TryGetProperty("summary", out var summaryProp))
						extractedSummary = summaryProp.GetString() ?? string.Empty;
					if (doc.RootElement.TryGetProperty("skills", out var skillsProp))
						finalSkillsJson = skillsProp.GetRawText();
				}
				catch { finalSkillsJson = "[]"; }
			}
			var existingAnalysis = await analysisRepository.FirstOrDefaultAsync(a => a.ResumeId == existingResume.Id);
			if (existingAnalysis is not null)
			{
				existingAnalysis.AtsScore = finalAts;
				existingAnalysis.SkillsJson = finalSkillsJson;
				//existingAnalysis.AtsAnalysisJson = atsStr;
				existingAnalysis.ExtractedSummary = extractedSummary;
				existingAnalysis.ExtractedTextPreview = resumeText.Length > 200 ? resumeText[..200] : resumeText;
				existingAnalysis.AnalyzedAt = DateTime.UtcNow;
				existingAnalysis.UpdatedAt = DateTime.UtcNow;
				analysisRepository.Update(existingAnalysis);
			}
			else
			{
				await analysisRepository.AddAsync(new ResumeAnalysis
				{
					ResumeId = existingResume.Id,
					AtsScore = finalAts,
					SkillsJson = finalSkillsJson,
					ExtractedSummary = extractedSummary,
					ExtractedTextPreview = resumeText.Length > 200 ? resumeText[..200] : resumeText,
					AnalyzedAt = DateTime.UtcNow
				});
			}
			var result = await analysisRepository.SaveChangesAsync();
			return result;
		}

		// old code
		//public async Task<bool> ProcessAsync(Resume existingResume, string resumeText, CancellationToken stoppingToken)
		//{
		//	resumeText = CustomExtensions.CleanBeforeChunking(resumeText);
		//	var chunks = CustomExtensions.ChunkByParagraph(resumeText, 2000);

		//	for (int i = 0; i < chunks.Count; i++)
		//	{
		//		var chunkText = chunks[i];
		//		var chunkEmbedding = await embedding.GetEmbeddingAsync(chunkText, stoppingToken);
		//		string chunkId = $"{existingResume.Id}_{i}";
		//		if (chunkEmbedding?.Length > 0)
		//		{
		//			await vectorService.UpsertAsync(config["Qdrant:QdrantCollections:Resumes"]!, chunkId, chunkEmbedding,
		//			new Dictionary<string, object>
		//			{
		//				["type"] = config["Supabase:StorageBucket"]!,
		//				["resume_id"] = existingResume.Id,
		//				["user_id"] = existingResume.UserId,
		//				["file_name"] = existingResume.FileName,
		//				["chunk_index"] = i,
		//				["chunk_text"] = chunkText
		//			});
		//		}
		//	}
		//	var skillsStr = await analysis.ExtractSkillsAsync(resumeText);
		//	var atsStr = await analysis.GetAtsScoreAsync(resumeText);

		//	string finalSkillsJson = "[]";
		//	decimal finalAts = 0;
		//	string extractedSummary = string.Empty;

		//	try
		//	{
		//		using var doc = System.Text.Json.JsonDocument.Parse(skillsStr);
		//		finalSkillsJson = skillsStr;
		//		if (doc.RootElement.TryGetProperty("summary", out var summaryProp))
		//			extractedSummary = summaryProp.GetString() ?? string.Empty;
		//	}
		//	catch { finalSkillsJson = "[]"; }

		//	try
		//	{
		//		using var doc = System.Text.Json.JsonDocument.Parse(atsStr);
		//		if (doc.RootElement.TryGetProperty("ats_score", out var scoreProp) && scoreProp.TryGetDecimal(out var parsedAts))
		//			finalAts = Math.Clamp(parsedAts, 0, 100);
		//	}
		//	catch { finalAts = 0; }

		//	// upsert (unique on resume_id)
		//	var existingAnalysis = await analysisRepository.FirstOrDefaultAsync(a => a.ResumeId == existingResume.Id);
		//	if (existingAnalysis is not null)
		//	{
		//		existingAnalysis.AtsScore = finalAts;
		//		existingAnalysis.SkillsJson = finalSkillsJson;
		//		existingAnalysis.AtsAnalysisJson = atsStr;
		//		existingAnalysis.ExtractedSummary = extractedSummary;
		//		existingAnalysis.ExtractedTextPreview = resumeText.Length > 200 ? resumeText[..200] : resumeText;
		//		existingAnalysis.AnalyzedAt = DateTime.UtcNow;
		//		existingAnalysis.UpdatedAt = DateTime.UtcNow;
		//		analysisRepository.Update(existingAnalysis);
		//	}
		//	else
		//	{
		//		await analysisRepository.AddAsync(new ResumeAnalysis
		//		{
		//			ResumeId = existingResume.Id,
		//			AtsScore = finalAts,
		//			SkillsJson = finalSkillsJson,
		//			ExtractedSummary = extractedSummary,
		//			ExtractedTextPreview = resumeText.Length > 200 ? resumeText[..200] : resumeText,
		//			AnalyzedAt = DateTime.UtcNow
		//		});
		//	}
		//	await analysisRepository.SaveChangesAsync();
		//	return true;
		//}



		// Helper block to keep metadata mapping DRY and clean
		private Dictionary<string, object> BuildMetadata(Resume resume, int index, string text) => new()
		{
			["type"] = config["Supabase:StorageBucket"]!,
			["resume_id"] = resume.Id,
			["user_id"] = resume.UserId,
			["file_name"] = resume.FileName,
			["chunk_index"] = index,
			["chunk_text"] = text
		};
	}
}
