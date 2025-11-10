using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace AnimalZoo.App.Data;

/// <summary>
/// Design-time factory for creating AnimalZooContext instances.
/// Required by EF Core tools for migrations and scaffolding.
/// </summary>
public class AnimalZooContextFactory : IDesignTimeDbContextFactory<AnimalZooContext>
{
    /// <summary>
    /// Creates a new instance of AnimalZooContext for design-time operations.
    /// </summary>
    public AnimalZooContext CreateDbContext(string[] args)
    {
        // Build configuration from appsettings.json
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        // Get connection string
        var connectionString = configuration.GetConnectionString("AnimalZooDb")
            ?? throw new InvalidOperationException("Connection string 'AnimalZooDb' not found in configuration.");

        // Create DbContextOptions
        var optionsBuilder = new DbContextOptionsBuilder<AnimalZooContext>();
        optionsBuilder.UseSqlServer(connectionString);

        return new AnimalZooContext(optionsBuilder.Options);
    }
}
