using Avalonia.Controls;
using AnimalZoo.App.Localization;

namespace AnimalZoo.App.Views;

/// <summary>
/// Simple modal alert window with a localized message and OK button.
/// Ensures that the OK button always uses the localized string from the current language.
/// </summary>
public partial class AlertWindow : Window
{
    public AlertWindow()
    {
        InitializeComponent();

        // Set localized title (shown in window title bar)
        Title = Loc.Instance["Dialog.Notice.Title"];

        // Ensure the OK button text is always localized (DynamicResource may not update on new windows)
        var okButton = this.FindControl<Button>("OkButton");
        if (okButton is not null)
            okButton.Content = Loc.Instance["Buttons.OK"];
    }

    /// <summary>
    /// Creates a new alert window with a given message and localized OK button text.
    /// </summary>
    /// <param name="message">The text message to display.</param>
    public AlertWindow(string message) : this()
    {
        this.FindControl<TextBlock>("MessageText")!.Text = message;
    }

    /// <summary>
    /// Closes the alert window when the OK button is clicked.
    /// </summary>
    private void OnOk(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => Close();
}