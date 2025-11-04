using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
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
        }

        base.OnFrameworkInitializationCompleted();
    }
}
