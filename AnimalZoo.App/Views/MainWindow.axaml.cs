using Avalonia.Controls;
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

    private async void OnAddAnimalClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm) return;

        var dialog = new AddAnimalWindow();
        var result = await dialog.ShowDialog<Models.Animal?>(this);
        if (result is not null)
            vm.AddAnimal(result);
    }
}