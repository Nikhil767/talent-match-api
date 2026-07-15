using ResumeAnalyzer.Background;
using ResumeAnalyzer.Domain.Constants;
using ResumeAnalyzer.Domain.Entities;
using ResumeAnalyzer.Domain.Repositories;
using ResumeAnalyzer.Middleware;
using ResumeAnalyzer.Services;
using ResumeAnalyzer.Services.Helper;
using System.Text.Json;

namespace ResumeAnalyzer.Endpoints
{
	public static class ResumeEndpoints
	{
		public static void MapResumeEndpoints(this WebApplication app)
		{
			var group = app.MapGroup("/api/resume").WithTags("Resume").RequireAuthorization();
			// Upload + parse PDF, extract skills, generate embedding, store
			group.MapPost("/upload", async (
			IFormFile file,
			HttpContext ctx,
			IResumeRepository resumeRepository,
			AnalysisQueue analysisQueue,
			SupabaseStorageRestService supabaseStorageRestService,
			IConfiguration config,
			CancellationToken ct) =>
			{
				var isValid = Validate(file, config, out string errorMessage);
				if (!isValid)
					return Results.BadRequest(errorMessage);
				var userId = ctx.User.GetGuidUserId();
				try
				{
					using var ms = new MemoryStream();
					await file.CopyToAsync(ms, ct);
					var bytes = ms.ToArray();

					var finalHash =  bytes.ComputeSha256Hex();
					if (string.IsNullOrWhiteSpace(finalHash))
						return Results.BadRequest(new { error = "Failed to generate the file hash" });
					var existingResume = await resumeRepository.FirstOrDefaultAsync(r => r.UserId == userId && r.FileHash == finalHash);
					if (existingResume is not null)
					{
						return Results.BadRequest(new
						{
							Id = existingResume.Id,
							Status = ResumeStatusValues.Duplicate,
							Message = "This file is already processed."
						});
					}

					var resumeId = Guid.NewGuid();
					string extension = Path.GetExtension(file.FileName).ToLower();
					string fileName = $"{userId}/{resumeId}{extension}";
					string bearerToken = string.Empty;// ctx.GetBearerToken();
					var publicUrl = await supabaseStorageRestService.UploadAsync(config["Supabase:StorageBucket"]!, fileName, ms, file.ContentType, bearerToken);

					// 5. Database State Synchronization
					var resumeEntity = new Resume
					{
						Id = resumeId,
						UserId = userId,
						FileName = file.FileName,
						FileHash = finalHash,
						StoragePath = publicUrl,
						MimeType = file.ContentType,
						FileSizeBytes = file.Length,
						Status = ResumeStatusValues.Queued,
						ProcessingStep = ProcessingStepValues.Uploaded,
						CreatedAt = DateTime.UtcNow,
						UpdatedAt = DateTime.UtcNow
					};
					await resumeRepository.AddAsync(resumeEntity);
					var isSaved = await resumeRepository.SaveChangesAsync();

					// 6. Push to Thread-Safe Background Worker Queue immediately
					await analysisQueue.QueueBackgroundWorkItemAsync(resumeEntity.Id, bearerToken);
					return Results.Accepted(value: new
					{
						Id = resumeEntity.Id,
						Status = resumeEntity.Status,
						Message = "Resume safely uploaded in a single-pass stream execution loop. Background pipeline initiated."
					});
				}
				catch (Exception ex)
				{
					//logger.LogError(ex, "An anomaly occurred in the Minimal API upload execution sequence.");
					return Results.StatusCode(StatusCodes.Status500InternalServerError);
				}
			}).DisableAntiforgery();

			// GET /resume — list all resumes for logged-in user
			group.MapGet("/", async (HttpContext ctx, IResumeRepository resumeRepository) =>
			{
				var userId = ctx.User.GetGuidUserId();
				var resumes = await resumeRepository.FindAsync(x => x.UserId == userId);
				return Results.Ok(resumes);
			})
			.WithSummary("List all resumes for current user");

			// GET /resume/{id}
			group.MapGet("/{id}", async (Guid id, HttpContext ctx, IResumeRepository resumeRepository) =>
			{
				var userId = ctx.User.GetGuidUserId();
				var resume = await resumeRepository.GetResumeAnalysisAsync(x => x.Id == id && x.UserId == userId);
				// need to give a dto of Resume & ResumeAnlaysis
				return resume is null ? Results.NotFound() : Results.Ok(resume);
			})
			.WithSummary("Get a single resume with extracted text and skills");

			// GET /resume/{id}/skills — just the skills JSON
			group.MapGet("/{id}/skills", async (Guid id, HttpContext ctx, IResumeRepository resumeRepository) =>
			{
				var userId = ctx.User.GetGuidUserId();
				var resume = await resumeRepository.GetResumeAnalysisAsync(x => x.Id == id && x.UserId == userId);
				if (resume is null) return Results.NotFound();
				var skills = JsonSerializer.Deserialize<object>(resume.SkillsJson);
				return Results.Ok(skills);
			})
			.WithSummary("Get extracted skills for a resume");

			// DELETE /resume/{id}
			group.MapDelete("/{id}", async (
				Guid id,
				HttpContext ctx,
				IResumeRepository resumeRepository,
				IConfiguration config,
				VectorService vector,
				SupabaseStorageRestService supabaseStorageRestService) =>
			{
				var userId = ctx.User.GetGuidUserId();
				var resume = await resumeRepository.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);
				if (resume is null) return Results.NotFound();

				string bearerToken = string.Empty;//ctx.GetBearerToken();
				await supabaseStorageRestService.DeleteAsync(config["Supabase:StorageBucket"]!, resume.StoragePath, bearerToken); // delete PDF from R2
				await vector.DeleteByResumeIdAsync(config["Qdrant:QdrantCollections:Resumes"]!, resume.Id.ToString()); // delete vector from Qdrant
				resumeRepository.Delete(resume);            // delete metadata from Supabase
				await resumeRepository.SaveChangesAsync();
				return Results.Ok(new { message = "Resume deleted" });
			})
			.WithSummary("Delete a resume (PDF + vector + metadata)");

			//		// POST /resume/build
			//		group.MapPost("/build", async (
			//ResumeExportRequest req,
			//IResumeExportService service) =>
			//		{
			//			var result = await service.ExportAsync(req);
			//			return Results.File(
			//				fileContents: result.FileBytes,
			//				contentType: result.FileType == "pdf" ? "application/pdf" :
			//							 "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
			//				fileDownloadName: result.FileName
			//			);
			//		});

			//		// POST /resume/export
			//		group.MapPost("/export", async (
			//ResumeExportRequest req,
			//IResumeExportService service) =>
			//		{
			//			var result = await service.ExportAsync(req);
			//			return Results.File(
			//				fileContents: result.FileBytes,
			//				contentType: result.FileType == "pdf" ? "application/pdf" :
			//							 "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
			//				fileDownloadName: result.FileName
			//			);
			//		});
		}

		#region Private Helper Methods

		public static bool Validate(IFormFile file, IConfiguration config, out string error)
		{
			error = string.Empty;
			var filesConfig = config.GetSection("Files");

			var allowedExtensions = filesConfig.GetSection("AllowedExtensions").Get<string[]>();
			var allowedMimeTypes = filesConfig.GetSection("AllowedMimeTypes").Get<string[]>();

			var minSizeKb = filesConfig.GetValue<int>("MinSizeKb");
			var maxSizeMb = filesConfig.GetValue<int>("MaxSizeMb");
			var maxFileNameLength = filesConfig.GetValue<int>("MaxFileNameLength");

			var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
			var mimeType = file.ContentType;

			// Validate extension
			if (!allowedExtensions.Contains(extension))
			{
				error = $"Invalid file type. Allowed: {string.Join(", ", allowedExtensions!)}";
				return false;
			}
			// Validate MIME type
			if (!allowedMimeTypes.Contains(mimeType))
			{
				error = $"Invalid MIME type: {mimeType}";
				return false;
			}

			// Validate file name length
			if (file.FileName.Length > maxFileNameLength)
			{
				error = $"File name too long. Max allowed: {maxFileNameLength} characters.";
				return false;
			}

			// Validate file size
			long minBytes = minSizeKb * 1024;
			long maxBytes = maxSizeMb * 1024 * 1024;

			if (file.Length < minBytes)
			{
				error = $"File too small. Minimum size: {minSizeKb} KB.";
				return false;
			}

			if (file.Length > maxBytes)
			{
				error = $"File too large. Maximum size: {maxSizeMb} MB.";
				return false;
			}
			return true;
		}
		#endregion Private Helper Methods
	}
}
