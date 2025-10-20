using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using AnimalZoo.App.ViewModels;

namespace AnimalZoo.App.Views;

/// <summary>
/// Main window code-behind: opens AddAnimal dialog, shows alerts.
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        // Subscribe to alert requests from VM
        this.Opened += (_, __) =>
        {
            if (DataContext is MainWindowViewModel vm)
            {
                vm.AlertRequested += async msg =>
                {
                    var alert = new AlertWindow(msg);
                    await alert.ShowDialog(this);
                };
            }
        };
    }

    /// <summary>
    /// Loads the XAML for this Window. This implementation avoids relying on generated InitializeComponent().
    /// </summary>
    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private async void OnAddAnimalClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm) return;

        var dialog = new AddAnimalWindow();
        var result = await dialog.ShowDialog<Models.Animal?>(this);
        if (result is not null)
            vm.AddAnimal(result);
    }
}