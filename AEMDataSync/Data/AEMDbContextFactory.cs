using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace AEMDataSync.Data
{
    public class AEMDbContextFactory : IDesignTimeDbContextFactory<AEMDbContext>
    {
        public AEMDbContext CreateDbContext(string[] args)
        {
            // Build configuration
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            // Get connection string
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            // Create options builder
            var optionsBuilder = new DbContextOptionsBuilder<AEMDbContext>();
            optionsBuilder.UseSqlServer(connectionString);

            return new AEMDbContext(optionsBuilder.Options);
        }
    }
}