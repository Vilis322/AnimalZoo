using System;
using System.IO;
using AnimalZoo.App.Interfaces;
using AnimalZoo.App.Logging;
using AnimalZoo.App.Repositories;
using AnimalZoo.App.ViewModels;
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
        var logFilePath = configuration["Logging:LogFilePath"] ?? "logs/animalzoo.log";

        // Make log path absolute if it's relative
        if (!Path.IsPathRooted(logFilePath))
        {
            logFilePath = Path.Combine(AppContext.BaseDirectory, logFilePath);
        }

        // Ensure log directory exists
        var logDirectory = Path.GetDirectoryName(logFilePath);
        if (!string.IsNullOrEmpty(logDirectory) && !Directory.Exists(logDirectory))
        {
            Directory.CreateDirectory(logDirectory);
        }

        if (loggerType.Equals("Xml", StringComparison.OrdinalIgnoreCase))
        {
            services.AddSingleton<ILogger>(new XmlLogger(logFilePath));
        }
        else
        {
            services.AddSingleton<ILogger>(new JsonLogger(logFilePath));
        }

        // Register repositories
        var connectionString = configuration.GetConnectionString("AnimalZooDb")
            ?? throw new InvalidOperationException("Connection string 'AnimalZooDb' not found in configuration.");

        services.AddSingleton<IAnimalsRepository>(new SqlAnimalsRepository(connectionString));
        services.AddSingleton<IEnclosureRepository>(new SqlEnclosureRepository(connectionString));

        // Register ViewModels
        services.AddTransient<MainWindowViewModel>();

        return services.BuildServiceProvider();
    }
}
