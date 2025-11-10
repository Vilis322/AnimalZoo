using System;
using System.IO;
using AnimalZoo.App.Data;
using AnimalZoo.App.Interfaces;
using AnimalZoo.App.Logging;
using AnimalZoo.App.Repositories;
using AnimalZoo.App.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AnimalZoo.App.Configuration;

/// <summary>
/// Configures dependency injection services for the application.
/// </summary>
public static class ServiceConfiguration
{
    /// <summary>
    /// Builds and configures the service provider with all dependencies.
    /// </summary>
    public static IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        // Build configuration from appsettings.json
        // Use AppContext.BaseDirectory to find the file relative to the executable location
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        // Register configuration
        services.AddSingleton<IConfiguration>(configuration);

        // Register logger based on configuration
        var loggerType = configuration["Logging:LoggerType"] ?? "Json";
        var jsonLogPath = configuration["Logging:JsonLogFilePath"] ?? "Logs/animalzoo.json";
        var xmlLogPath = configuration["Logging:XmlLogFilePath"] ?? "Logs/animalzoo.xml";

        // Find project root and resolve paths relative to it
        var projectRoot = FindProjectRoot();
        if (!Path.IsPathRooted(jsonLogPath))
        {
            jsonLogPath = Path.Combine(projectRoot, jsonLogPath);
        }
        if (!Path.IsPathRooted(xmlLogPath))
        {
            xmlLogPath = Path.Combine(projectRoot, xmlLogPath);
        }

        // Ensure log directories exist
        var jsonLogDirectory = Path.GetDirectoryName(jsonLogPath);
        if (!string.IsNullOrEmpty(jsonLogDirectory) && !Directory.Exists(jsonLogDirectory))
        {
            Directory.CreateDirectory(jsonLogDirectory);
        }
        var xmlLogDirectory = Path.GetDirectoryName(xmlLogPath);
        if (!string.IsNullOrEmpty(xmlLogDirectory) && !Directory.Exists(xmlLogDirectory))
        {
            Directory.CreateDirectory(xmlLogDirectory);
        }

        // Create logger based on configuration
        ILogger logger;
        if (loggerType.Equals("Both", StringComparison.OrdinalIgnoreCase))
        {
            // Use composite logger for both formats
            Console.WriteLine("[Logging] Type: Both (JSON + XML)");
            Console.WriteLine($"[Logging] JSON Path: {jsonLogPath}");
            Console.WriteLine($"[Logging] XML Path: {xmlLogPath}");
            logger = new CompositeLogger(
                new JsonLogger(jsonLogPath),
                new XmlLogger(xmlLogPath)
            );
        }
        else if (loggerType.Equals("Xml", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine("[Logging] Type: Xml");
            Console.WriteLine($"[Logging] Path: {xmlLogPath}");
            logger = new XmlLogger(xmlLogPath);
        }
        else
        {
            Console.WriteLine("[Logging] Type: Json");
            Console.WriteLine($"[Logging] Path: {jsonLogPath}");
            logger = new JsonLogger(jsonLogPath);
        }

        services.AddSingleton<ILogger>(logger);

        // Register repositories based on configuration
        var connectionString = configuration.GetConnectionString("AnimalZooDb")
            ?? throw new InvalidOperationException("Connection string 'AnimalZooDb' not found in configuration.");

        var repositoryType = configuration["DataAccess:RepositoryType"] ?? "AdoNet";

        if (repositoryType.Equals("EfCore", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine("[Data Access] Type: Entity Framework Core");

            // Register DbContext
            services.AddDbContext<AnimalZooContext>(options =>
                options.UseSqlServer(connectionString));

            // Register EF Core repositories
            services.AddScoped<IAnimalsRepository, EfAnimalsRepository>();
            services.AddScoped<IEnclosureRepository, EfEnclosureRepository>();
        }
        else
        {
            Console.WriteLine("[Data Access] Type: ADO.NET");

            // Register ADO.NET repositories
            services.AddSingleton<IAnimalsRepository>(new SqlAnimalsRepository(connectionString));
            services.AddSingleton<IEnclosureRepository>(new SqlEnclosureRepository(connectionString));
        }

        // Register ViewModels
        services.AddTransient<MainWindowViewModel>();

        return services.BuildServiceProvider();
    }

    /// <summary>
    /// Finds the project root directory by searching for the .sln file.
    /// </summary>
    /// <returns>The absolute path to the project root directory.</returns>
    private static string FindProjectRoot()
    {
        var currentDirectory = new DirectoryInfo(AppContext.BaseDirectory);

        // Search up the directory tree for the solution file
        while (currentDirectory != null)
        {
            if (currentDirectory.GetFiles("*.sln").Length > 0)
            {
                return currentDirectory.FullName;
            }
            currentDirectory = currentDirectory.Parent;
        }

        // Fallback to base directory if .sln not found
        return AppContext.BaseDirectory;
    }
}
