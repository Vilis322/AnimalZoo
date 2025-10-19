using Avalonia.Controls;

namespace AnimalZoo.App.Views;

/// <summary>
/// Simple modal alert window with a message and OK button.
/// </summary>
public partial class AlertWindow : Window
{
    public AlertWindow()
    {
        InitializeComponent();
    }
    
    public AlertWindow(string message) : this()
    {
        this.FindControl<TextBlock>("MessageText")!.Text = message;
    }

    private void OnOk(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => Close();
}