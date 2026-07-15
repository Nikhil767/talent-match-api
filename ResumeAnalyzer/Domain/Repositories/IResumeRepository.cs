using System.Linq.Expressions;
using ResumeAnalyzer.Domain.Entities;

namespace ResumeAnalyzer.Domain.Repositories
{
	public interface IResumeRepository : IRepository<Resume>
	{
		Task<Resume?> GetByHashAsync(string hash, CancellationToken ct = default);
		Task<ResumeAnalysis?> GetResumeAnalysisAsync(Expression<Func<Resume, bool>> predicate, CancellationToken ct = default);
	}
}
