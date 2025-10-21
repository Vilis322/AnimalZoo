using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using AnimalZoo.App.ViewModels;

namespace AnimalZoo.App.Views;

/// <summary>
/// Main window code-behind: opens AddAnimal dialog, shows alerts (with dedup & safe wiring).
/// </summary>
public partial class MainWindow : Window
{
    // Prevent duplicate subscriptions to VM.AlertRequested
    private bool _alertsWired;

    // Dedup state for alerts
    private string? _lastAlertMessage;
    private DateTime _lastAlertAtUtc = DateTime.MinValue;

    public MainWindow()
    {
        InitializeComponent();

        // Subscribe once when the window is opened
        this.Opened += (_, __) => WireAlertsIfNeeded();

        // Also handle DataContext changes gracefully:
        this.DataContextChanged += (_, __) =>
        {
            _alertsWired = false;
            WireAlertsIfNeeded();
        };
    }

    /// <summary>
    /// Loads the XAML for this Window. This implementation avoids relying on generated InitializeComponent().
    /// </summary>
    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    /// <summary>
    /// Ensures we subscribe to the current VM's AlertRequested exactly once.
    /// </summary>
    private void WireAlertsIfNeeded()
    {
        if (_alertsWired) return;
        if (DataContext is not MainWindowViewModel vm) return;

        vm.AlertRequested += async msg =>
        {
            if (ShouldSuppressDuplicate(msg))
                return;

            var alert = new AlertWindow(msg);
            await alert.ShowDialog(this);

            _lastAlertMessage = msg;
            _lastAlertAtUtc = DateTime.UtcNow;
        };

        _alertsWired = true;
    }

    /// <summary>
    /// Returns true when an identical alert was just shown recently.
    /// This collapses back-to-back duplicates (e.g., second popup on closing the first).
    /// </summary>
    private bool ShouldSuppressDuplicate(string message)
    {
        const int duplicateWindowMs = 600; // tweakable if нужно
        if (_lastAlertMessage is null) return false;

        var same = string.Equals(_lastAlertMessage, message, StringComparison.Ordinal);
        var recent = (DateTime.UtcNow - _lastAlertAtUtc).TotalMilliseconds < duplicateWindowMs;

        return same && recent;
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
