using Avalonia;

namespace AnimalZoo.App;

/// <summary>
/// Program entry point.
/// </summary>
internal static class Program
{
    /// <summary>
    /// Main entry. Configures and starts Avalonia app.
    /// </summary>
    public static void Main(string[] args)
    {
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
