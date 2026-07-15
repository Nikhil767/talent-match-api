using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using ResumeAnalyzer.Domain.Entities;

namespace ResumeAnalyzer.Domain.Repositories
{
	public class ResumeRepository : Repository<Resume>, IResumeRepository
	{
		public ResumeRepository(AppDbContext context) : base(context) { }

		public async Task<Resume?> GetByHashAsync(string hash, CancellationToken ct = default)
		{
			return await _context.Resumes.FirstOrDefaultAsync(r => r.FileHash == hash, ct);
		}

		public async Task<ResumeAnalysis?> GetResumeAnalysisAsync(Expression<Func<Resume, bool>> predicate, CancellationToken ct = default)
		{
			var resumeId = await _context.Resumes.Where(predicate).Select(r => r.Id).FirstOrDefaultAsync(ct);
			if (resumeId == Guid.Empty)
				return null;
			return await _context.ResumeAnalyses.FirstOrDefaultAsync(x => x.ResumeId == resumeId, ct);
		}
	}
}
