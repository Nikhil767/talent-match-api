using System.Linq.Expressions;

namespace ResumeAnalyzer.Domain.Repositories
{
	public interface IRepository<T> where T : class
	{
		ValueTask<T?> GetByIdAsync(Guid id);
		Task<List<T>> GetAllAsync(CancellationToken ct = default);
		Task<List<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);
		Task<T> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);
		Task AddAsync(T entity, CancellationToken ct = default);
		void Update(T entity);
		void Delete(T entity);
		Task<bool> SaveChangesAsync(CancellationToken ct = default);
		Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);
	}
}
