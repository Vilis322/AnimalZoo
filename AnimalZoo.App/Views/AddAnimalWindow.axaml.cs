using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using AnimalZoo.App.Utils;
using AnimalZoo.App.ViewModels;

namespace AnimalZoo.App.Views
{
    /// <summary>
    /// Dialog for creating a new animal (extensible via reflection).
    /// Validates name and age, keeps the dialog open on invalid input.
    /// Also normalizes '.' to ',' while typing in numeric fields so both separators work.
    /// </summary>
    public partial class AddAnimalWindow : Window
    {
        public AddAnimalWindow()
        {
            InitializeComponent();

            // Normalize '.' to ',' for any TextBox inside this window (useful for NumericUpDown inner textbox).
            // This way the user can type either decimal separator; parsing will succeed under comma-based cultures.
            AddHandler(InputElement.TextInputEvent, OnTextInputNormalizeDotToComma, RoutingStrategies.Tunnel);
        }

        /// <summary>
        /// Replaces a typed '.' with ',' in the currently focused TextBox.
        /// This keeps spinner buttons and culture-aware parsing intact (we do not change XAML or control culture).
        /// </summary>
        private void OnTextInputNormalizeDotToComma(object? sender, TextInputEventArgs e)
        {
            if (!string.Equals(e.Text, ".", StringComparison.Ordinal))
                return;

            // Mark as handled and inject a comma into the focused TextBox.
            e.Handled = true;

            var top = TopLevel.GetTopLevel(this);
            var focused = top?.FocusManager?.GetFocusedElement();

            if (focused is TextBox tb)
            {
                var text = tb.Text ?? string.Empty;
                var start = tb.SelectionStart;
                var end = tb.SelectionEnd;

                // Replace selection with comma, or insert at caret if no selection.
                if (end > start)
                {
                    text = text.Remove(start, end - start).Insert(start, ",");
                    tb.Text = text;
                    tb.CaretIndex = start + 1;
                }
                else
                {
                    if (start < 0 || start > text.Length) start = text.Length;
                    text = text.Insert(start, ",");
                    tb.Text = text;
                    tb.CaretIndex = start + 1;
                }
            }
        }

        private void OnCancel(object? sender, RoutedEventArgs e)
        {
            Close(null);
        }

        private async void OnOk(object? sender, RoutedEventArgs e)
        {
            if (DataContext is not AddAnimalViewModel vm)
            {
                Close(null);
                return;
            }

            var name = vm.Name?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(name))
            {
                // Name is mandatory — keep dialog open and show alert.
                var alert = new AlertWindow("Unable to create an animal without a name. Please enter the animal's name!");
                await alert.ShowDialog(this);
                return;
            }

            // Age must be non-negative; vm.Age reflects parsed value from the numeric input via binding.
            if (vm.Age < 0 || !vm.IsAgeValid)
            {
                var alert = new AlertWindow("You entered a negative age. Please enter a valid age.");
                await alert.ShowDialog(this);
                return;
            }

            // Selected type is required
            var type = vm.SelectedType?.UnderlyingType;
            if (type is null)
            {
                var alert = new AlertWindow("Please select an animal type.");
                await alert.ShowDialog(this);
                return;
            }

            // Create via factory — pass double age (fractional support).
            var created = AnimalFactory.Create(type, name, vm.Age);
            Close(created);
        }
    }
}
