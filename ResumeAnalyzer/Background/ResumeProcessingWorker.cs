using Microsoft.EntityFrameworkCore;
using ResumeAnalyzer.Domain;
using ResumeAnalyzer.Domain.Constants;
using ResumeAnalyzer.Services.Facade;

namespace ResumeAnalyzer.Background
{
	public class ResumeProcessingWorker : BackgroundService
	{
		private readonly AnalysisQueue _queue;
		private readonly IServiceProvider _serviceProvider;
		private readonly ILogger<ResumeProcessingWorker> _logger;

		public ResumeProcessingWorker(AnalysisQueue queue, IServiceProvider serviceProvider, ILogger<ResumeProcessingWorker> logger)
		{
			_queue = queue;
			_serviceProvider = serviceProvider;
			_logger = logger;
		}

		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			_logger.LogInformation("Resume Background Processor initialized.");
			// Recovery: Scan for any records stranded as 'queued' if the AppPool restarted
			await RecoverDanglingJobsAsync();
			while (!stoppingToken.IsCancellationRequested)
			{
				Guid recordId = Guid.Empty;
				string bearerToken = string.Empty;
				try
				{
					// Wait for the next item from the Controller entry point
					(recordId, bearerToken) = await _queue.DequeueAsync(stoppingToken);

					// Create a dynamic Scope since AppDbContext and Facades are Scoped
					using var scope = _serviceProvider.CreateScope();
					var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

					// 1. Atomic Check & Status Change: queued -> inprogress
					bool isAcquired = await TryTransitionToProcessingAsync(dbContext, recordId);
					if (!isAcquired) continue;

					_logger.LogInformation($"Job {recordId} transitioned to 'inprogress'. Initiating Facade Pipeline...");

					// 2. Resolve Facade Pipeline and execute text mining/AI tasks
					var pipelineService = scope.ServiceProvider.GetRequiredService<ResumePipelineService>();
					var result = await pipelineService.ProcessAsync(recordId, bearerToken, stoppingToken);

					_logger.LogInformation($"Job {recordId} executed successfully by Facade. result : {result}");
				}
				catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
				{
					break;
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, $"Critical fault handling tracking ID: {recordId}");
					if (recordId != Guid.Empty)
					{
						using var scope = _serviceProvider.CreateScope();
						var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
						await UpdateStatusToFailedAsync(dbContext, recordId);
					}
					// Prevent tight failure hot-looping
					await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
				}
			}
		}

		private async Task<bool> TryTransitionToProcessingAsync(AppDbContext dbContext, Guid id)
		{
			var resume = await dbContext.Resumes.FirstOrDefaultAsync(r => r.Id == id &&
			r.Status == ResumeStatusValues.Queued && r.AttemptCount < 3);
			if (resume == null) return false;

			resume.AttemptCount += 1;
			resume.Status = ResumeStatusValues.Processing;
			resume.LockedAt = resume.UpdatedAt = DateTime.UtcNow;

			return await dbContext.SaveChangesAsync() > 0;
			//return true;
		}

		private async Task UpdateStatusToFailedAsync(AppDbContext dbContext, Guid id)
		{
			try
			{
				var resume = await dbContext.Resumes.FindAsync(id);
				if (resume != null)
				{
					resume.Status = ResumeStatusValues.Failed;
					resume.UpdatedAt = DateTime.UtcNow;
					await dbContext.SaveChangesAsync();
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to flag status back to failed state.");
			}
		}

		private async Task RecoverDanglingJobsAsync()
		{
			try
			{
				using var scope = _serviceProvider.CreateScope();
				var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

				var strandedJobs = await dbContext.Resumes
					.Where(r => r.Status == ResumeStatusValues.Queued)
					.OrderBy(r => r.CreatedAt)
					.Select(r => r.Id)
					.ToListAsync();

				foreach (var id in strandedJobs)
				{
					await _queue.QueueBackgroundWorkItemAsync(id, string.Empty);
				}

				if (strandedJobs.Any())
					_logger.LogInformation($"Recovered {strandedJobs.Count} unprocessed items from database logs.");
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error executing app startup baseline recovery scanning.");
			}
		}

		//protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		//{
		//	_logger.LogInformation("Resume Processing Worker started.");

		//	// 1. Startup Sync: Catch up on any dangling jobs left over from a previous crash/shutdown
		//	await RecoverDanglingJobsAsync();

		//	// 2. Main Processing Loop
		//	while (!stoppingToken.IsCancellationRequested)
		//	{
		//		try
		//		{
		//			Guid recordId = await _queue.DequeueAsync(stoppingToken);

		//			// Use a scoped provider since NpgsqlDataSource/DbContext should be scoped
		//			using var scope = _serviceProvider.CreateScope();
		//			var dataSource = scope.ServiceProvider.GetRequiredService<NpgsqlDataSource>();

		//			// Atomic Check & State Transition
		//			bool lockAcquired = await TryTransitionToInProgressAsync(dataSource, recordId);

		//			if (!lockAcquired) continue; // Already picked up by someone else or missing

		//			_logger.LogInformation($"Processing record {recordId}...");

		//			// Execute heavy lifting
		//			await ExecuteAnalysisPipelineAsync(dataSource, recordId, stoppingToken);
		//		}
		//		catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
		//		{
		//			break;
		//		}
		//		catch (Exception ex)
		//		{
		//			_logger.LogError(ex, "Error processing resume item.");
		//			await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken); // Prevent hot loop on failure
		//		}
		//	}
		//}

		//private async Task<bool> TryTransitionToInProgressAsync(NpgsqlDataSource dataSource, Guid id)
		//{
		//	const string query = @"
		//          UPDATE resumes 
		//          SET status = 'inprogress', updated_at = NOW() 
		//          WHERE id = @id AND status = 'queued';";

		//	await using var cmd = dataSource.CreateCommand(query);
		//	cmd.Parameters.AddWithValue("id", id);

		//	int rowsAffected = await cmd.ExecuteNonQueryAsync();
		//	return rowsAffected > 0;
		//}

		//private async Task ExecuteAnalysisPipelineAsync(NpgsqlDataSource dataSource, Guid id, CancellationToken ct)
		//{
		//	try
		//	{
		//		// 1. Fetch paths from Supabase, download binary from Cloudflare R2
		//		// 2. Extract Text (Using free libraries: UglyToad.PdfPig for PDF, DocumentFormat.OpenXml for Docx)

		//		// 3. REMAINING ACTIONS:
		//		// - Resume Embedding & Job Embedding -> Use free local embeddings (like Microsoft.Extensions.AI and Ollama or a free HuggingFace API key)
		//		// - Extract Skills, Gaps, and Tailored Match details using LLM.

		//		// 4. Update status to 'completed' and save results
		//		await UpdateStatusAsync(dataSource, id, "completed", resultsJson: "{}");
		//	}
		//	catch (Exception ex)
		//	{
		//		_logger.LogError(ex, $"Failed pipeline execution for {id}");
		//		await UpdateStatusAsync(dataSource, id, "failed", null);
		//	}
		//}

		//private async Task UpdateStatusAsync(NpgsqlDataSource dataSource, Guid id, string status, string? resultsJson)
		//{
		//	const string query = @"
		//          UPDATE resumes 
		//          SET status = @status, results = @results, updated_at = NOW() 
		//          WHERE id = @id;";

		//	await using var cmd = dataSource.CreateCommand(query);
		//	cmd.Parameters.AddWithValue("status", status);
		//	cmd.Parameters.AddWithValue("results", resultsJson ?? (object)DBNull.Value);
		//	cmd.Parameters.AddWithValue("id", id);
		//	await cmd.ExecuteNonQueryAsync();
		//}

		//private async Task RecoverDanglingJobsAsync()
		//{
		//	using var scope = _serviceProvider.CreateScope();
		//	var dataSource = scope.ServiceProvider.GetRequiredService<NpgsqlDataSource>();

		//	const string query = "SELECT id FROM resumes WHERE status = 'queued' ORDER BY created_at ASC;";
		//	await using var cmd = dataSource.CreateCommand(query);
		//	await using var reader = await cmd.ExecuteReaderAsync();

		//	while (await reader.ReadAsync())
		//	{
		//		Guid id = reader.GetGuid(0);
		//		await _queue.QueueBackgroundWorkItemAsync(id);
		//	}
		//}
	}
}
