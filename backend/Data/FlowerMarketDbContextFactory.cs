using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace backend.Data
{
    public class FlowerMarketDbContextFactory : IDesignTimeDbContextFactory<FlowerMarketDbContext>
    {
        public FlowerMarketDbContext CreateDbContext(string[] args)
        {
            // Charger manuellement appsettings.json
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false)
                .Build();

            var optionsBuilder = new DbContextOptionsBuilder<FlowerMarketDbContext>();

            optionsBuilder.UseSqlServer(config.GetConnectionString("DefaultConnection"));

            return new FlowerMarketDbContext(optionsBuilder.Options);
        }
    }
}