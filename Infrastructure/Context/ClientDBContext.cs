using Domain.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Context
{
    public class ClientDBContext : DbContext
    {
        private readonly IHttpContextAccessor? _httpContextAccessor;
        private readonly IConfiguration? _configuration;

        //public string? DatabaseName { get; set; }
        //public string? ConnectionString { get; set; }

        public ClientDBContext(DbContextOptions<ClientDBContext> options, IConfiguration? configuration = null, IHttpContextAccessor? httpContextAccessor = null)
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

        public DbSet<User> Users { get; set; }
    }
}
