using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using AnimalZoo.App.Interfaces;
using AnimalZoo.App.Views;
using AnimalZoo.App.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace AnimalZoo.App;

/// <summary>
/// Avalonia Application class.
/// </summary>
public sealed partial class App : Application
{
    /// <inheritdoc />
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    /// <inheritdoc />
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Resolve MainWindowViewModel from DI container
            var vm = Program.ServiceProvider?.GetService<MainWindowViewModel>()
                ?? new MainWindowViewModel();

            desktop.MainWindow = new MainWindow
            {
                DataContext = vm
            };

            // Ensure logger is disposed on exit
            desktop.Exit += OnExit;
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void OnExit(object? sender, ControlledApplicationLifetimeExitEventArgs e)
    {
        // Dispose logger to flush remaining entries
        var logger = Program.ServiceProvider?.GetService<ILogger>();
        logger?.Dispose();
    }
}
