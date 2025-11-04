using System;
using Avalonia;
using AnimalZoo.App.Configuration;

namespace AnimalZoo.App;

/// <summary>
/// Program entry point.
/// </summary>
internal static class Program
{
    /// <summary>
    /// Global service provider for dependency injection.
    /// </summary>
    public static IServiceProvider? ServiceProvider { get; private set; }

    /// <summary>
    /// Main entry. Configures and starts Avalonia app.
    /// </summary>
    public static void Main(string[] args)
    {
        // Initialize dependency injection container
        ServiceProvider = ServiceConfiguration.ConfigureServices();

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    /// <summary>
    /// Avalonia app builder.
    /// </summary>
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace();
}
