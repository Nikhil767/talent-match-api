using ResumeAnalyzer.Domain.Entities;

namespace ResumeAnalyzer.Domain.Repositories
{
	public class ResumeJobMatchRepository : Repository<ResumeJobMatch>, IResumeJobMatchRepository
	{
		public ResumeJobMatchRepository(AppDbContext context) : base(context) { }
	}
}
