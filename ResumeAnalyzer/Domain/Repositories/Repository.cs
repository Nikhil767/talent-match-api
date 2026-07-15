using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace ResumeAnalyzer.Domain.Repositories
{
	public abstract class Repository<T> : IRepository<T> where T : class
	{
		protected readonly AppDbContext _context;
		protected readonly DbSet<T> _dbSet;

		protected Repository(AppDbContext context)
		{
			_context = context;
			_dbSet = _context.Set<T>();
		}

		public virtual ValueTask<T?> GetByIdAsync(Guid id) => _dbSet.FindAsync(id);

		public virtual Task<List<T>> GetAllAsync(CancellationToken ct = default) => _dbSet.ToListAsync(ct);

		public virtual Task<List<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default) =>
			_dbSet.Where(predicate).ToListAsync(ct);

		public virtual Task AddAsync(T entity, CancellationToken ct = default) => _dbSet.AddAsync(entity, ct).AsTask();

		public virtual void Update(T entity) => _dbSet.Update(entity);

		public virtual void Delete(T entity) => _dbSet.Remove(entity);

		public virtual async Task<bool> SaveChangesAsync(CancellationToken ct = default) => await _context.SaveChangesAsync(ct) > 0;

		public Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default)
		{
			return _dbSet.AnyAsync(predicate, ct);
		}

		public Task<T> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default)
		{
			return _dbSet.FirstOrDefaultAsync(predicate, ct);
		}
	}
}
