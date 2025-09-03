using Domain.Models.Entities.Answers;
using Domain.Models.Entities.Questions;
using Domain.Models.Entities.Quiz;
using Domain.Models.Entities.Token;
using Domain.Models.Entities.Users;
using Domain.Models.Entities.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Domain.Models.BaseEntities;

namespace Infrastructure.Context
{
    public class AppDBContext : DbContext
    {
        private readonly IHttpContextAccessor? _httpContextAccessor;
        private readonly IConfiguration? _configuration;

        public AppDBContext(DbContextOptions<AppDBContext> options, IConfiguration? configuration = null, IHttpContextAccessor? httpContextAccessor = null)
            : base(options)
        {
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();
        }
        public override int SaveChanges()
        {
            var entries = ChangeTracker.Entries<BaseEntity>();
            foreach (var entry in entries)
            {
                if (entry.State == EntityState.Added)
                {
                    entry.Entity.AddedDate = DateTime.UtcNow;
                    entry.Entity.UpdatedDate = DateTime.UtcNow;
                }
                else if (entry.State == EntityState.Modified)
                {
                    entry.Entity.UpdatedDate = DateTime.UtcNow;
                }
            }
            return base.SaveChanges();
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var entries = ChangeTracker.Entries<BaseEntity>();
            foreach (var entry in entries)
            {
                if (entry.State == EntityState.Added)
                {
                    entry.Entity.AddedDate = DateTime.UtcNow;
                    entry.Entity.UpdatedDate = DateTime.UtcNow;
                }
                else if (entry.State == EntityState.Modified)
                {
                    entry.Entity.UpdatedDate = DateTime.UtcNow;
                }
            }
            return await base.SaveChangesAsync(cancellationToken);
        }

        public DbSet<User> Users { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<Quiz> Quizzes { get; set; }
        public DbSet<Question> Questions { get; set; }
        public DbSet<Option> Options { get; set; }
        public DbSet<Answer> Answers { get; set; }
        public DbSet<Language> Languages { get; set; }
    }
}
