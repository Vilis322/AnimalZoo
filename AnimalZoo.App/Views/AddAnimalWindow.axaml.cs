using Avalonia.Controls;
using AnimalZoo.App.Models;
using AnimalZoo.App.Utils;
using AnimalZoo.App.ViewModels;

namespace AnimalZoo.App.Views;

/// <summary>
/// Dialog for creating a new animal (extensible via reflection).
/// </summary>
public partial class AddAnimalWindow : Window
{
    public AddAnimalWindow()
    {
        InitializeComponent();
    }

    private void OnCancel(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Close(null);
    }

    private void OnOk(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is not AddAnimalViewModel vm) { Close(null); return; }
        var created = AnimalFactory.Create(vm.SelectedType?.UnderlyingType, vm.Name?.Trim() ?? "", (int)vm.Age);
        Close(created);
    }
}