using Microsoft.EntityFrameworkCore;
using ResumeAnalyzer.Domain.Entities;
using System.Linq.Expressions;

namespace ResumeAnalyzer.Domain.Repositories
{
	public class JobRepository : Repository<Job>, IJobRepository
	{
		public JobRepository(AppDbContext context) : base(context) { }

		public async Task<List<Job>?> SearchJobsAsync(string query, CancellationToken ct = default)
		{
			if (string.IsNullOrWhiteSpace(query))
				return [];
			query = query.ToLower(); // normalize for case-insensitive search
			return await _context.Jobs.AsNoTracking().Where(j =>
					EF.Functions.ILike(j.Title!, $"%{query}%") ||
					EF.Functions.ILike(j.Company!, $"%{query}%"))
				.OrderByDescending(j => j.CreatedAt)
				.Take(20)
				.ToListAsync(ct);
		}

		public Task<List<string>> GetExistingJobIdsAsync(IEnumerable<string> jobIds, CancellationToken ct = default)
		{
			return _context.Jobs
				.AsNoTracking()
				.Where(j => jobIds.Contains(j.JobId))
				.Select(j => j.JobId)
				.ToListAsync(ct);
		}

		public Task<Job> GetJobsAsync(Expression<Func<Job, bool>> predicate, CancellationToken ct = default)
		{
			return _context.Jobs.AsNoTracking().FirstOrDefaultAsync(predicate, ct);
		}
	}
}
