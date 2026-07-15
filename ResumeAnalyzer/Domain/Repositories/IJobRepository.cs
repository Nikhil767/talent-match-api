using System.Linq.Expressions;
using ResumeAnalyzer.Domain.Entities;

namespace ResumeAnalyzer.Domain.Repositories
{
	public interface IJobRepository : IRepository<Job>
	{
		Task<List<Job>?> SearchJobsAsync(string query, CancellationToken ct = default);
		Task<List<string>> GetExistingJobIdsAsync(IEnumerable<string> jobIds, CancellationToken ct = default);
		Task<Job> GetJobsAsync(Expression<Func<Job, bool>> predicate, CancellationToken ct = default);
	}
}
