using Infrastructure.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace Infrastructure.Factories
{
    public class ClientDbContextFactory : IDesignTimeDbContextFactory<ClientDBContext>
    {
        public ClientDBContext CreateDbContext(string[] args)
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();

            var connectionString = config.GetConnectionString("ConnectionString");

            var optionsBuilder = new DbContextOptionsBuilder<ClientDBContext>();
            optionsBuilder.UseSqlServer(connectionString);

            return new ClientDBContext(optionsBuilder.Options);
        }
    }
}
