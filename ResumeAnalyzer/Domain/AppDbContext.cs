using Microsoft.EntityFrameworkCore;
using ResumeAnalyzer.Domain.Entities;

namespace ResumeAnalyzer.Domain
{
	public class AppDbContext : DbContext
	{
		public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
		public DbSet<Resume> Resumes => Set<Resume>();
		public DbSet<ResumeAnalysis> ResumeAnalyses => Set<ResumeAnalysis>();
		//public DbSet<ResumeChunk> ResumeChunks => Set<ResumeChunk>();
		public DbSet<Job> Jobs => Set<Job>();
		public DbSet<ResumeJobMatch> ResumeJobMatches => Set<ResumeJobMatch>();
		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			base.OnModelCreating(modelBuilder);

			// Map PostgreSQL Vector extension capability cleanly
			//modelBuilder.HasPostgresExtension("vector");
			//modelBuilder.HasPostgresExtension("pgcrypto");
			// Enforce Multi-column Constraints from schema.sql
			modelBuilder.Entity<Resume>()
				.HasIndex(r => new { r.UserId, r.FileHash })
				.IsUnique();

			//modelBuilder.Entity<ResumeChunk>()
			//	.HasIndex(rc => new { rc.ResumeId, rc.ChunkIndex })
			//	.IsUnique();
		}
	}
}
