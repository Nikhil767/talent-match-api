using Microsoft.EntityFrameworkCore;
using ResumeAnalyzer.Domain.Entities;
using System.Linq.Expressions;

namespace ResumeAnalyzer.Domain.Repositories
{
	public class ResumeAnalysisRepository : Repository<ResumeAnalysis>, IResumeAnalysisRepository
	{
		public ResumeAnalysisRepository(AppDbContext context) : base(context) { }
	}
}
