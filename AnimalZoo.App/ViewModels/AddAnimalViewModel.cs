using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using AnimalZoo.App.Utils;

namespace AnimalZoo.App.ViewModels
{
    /// <summary>
    /// ViewModel for AddAnimalWindow: holds user input and type list.
    /// Provides culture-agnostic age parsing (supports both ',' and '.').
    /// </summary>
    public sealed class AddAnimalViewModel : INotifyPropertyChanged
    {
        private string _name = string.Empty;

        // Numeric age actually used to construct the Animal (fractional supported).
        private double _age = 1.0;

        // Text entered by user; we parse it into Age allowing both ',' and '.'.
        private string _ageInput = "1";

        // Uses AnimalTypeOption from AnimalFactory (has DisplayName + UnderlyingType).
        private AnimalTypeOption? _selectedType;

        public ObservableCollection<AnimalTypeOption> AnimalTypes { get; } = new();

        /// <summary>User-entered name (must be non-empty on OK).</summary>
        public string Name
        {
            get => _name;
            set
            {
                if (_name != value)
                {
                    _name = value ?? string.Empty;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Parsed numeric age in years (can be fractional). Do not bind UI directly to this — use AgeInput.
        /// </summary>
        public double Age
        {
            get => _age;
            private set
            {
                if (value < 0)
                    return; // Negative ages are rejected here; dialog handles alert on OK.

                if (Math.Abs(_age - value) > double.Epsilon)
                {
                    _age = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Text field bound to the age TextBox. Supports both ',' and '.' as decimal separators.
        /// </summary>
        public string AgeInput
        {
            get => _ageInput;
            set
            {
                if (_ageInput == value) return;
                _ageInput = value ?? string.Empty;

                if (TryParseAge(_ageInput, out var parsed) && parsed >= 0)
                    Age = parsed;

                OnPropertyChanged();
                OnPropertyChanged(nameof(IsAgeValid));
            }
        }

        /// <summary>Selected type to create (e.g., Cat, Dog, Bird).</summary>
        public AnimalTypeOption? SelectedType
        {
            get => _selectedType;
            set
            {
                if (!Equals(_selectedType, value))
                {
                    _selectedType = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>True if AgeInput parses to a non-negative number.</summary>
        public bool IsAgeValid => TryParseAge(_ageInput, out var v) && v >= 0;

        /// <summary>Parses a text into a double, accepting either ',' or '.' as decimal separator.</summary>
        private static bool TryParseAge(string? text, out double value)
        {
            value = 0;
            if (string.IsNullOrWhiteSpace(text)) return false;

            var normalized = text.Trim().Replace(',', '.');
            return double.TryParse(
                normalized,
                NumberStyles.Float | NumberStyles.AllowThousands,
                CultureInfo.InvariantCulture,
                out value);
        }

        public AddAnimalViewModel()
        {
            foreach (var opt in AnimalFactory.GetAvailableAnimalTypes())
                AnimalTypes.Add(opt);

            SelectedType = AnimalTypes.FirstOrDefault();
            Age = 1.0;
            AgeInput = "1"; // keep input in sync
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? prop = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
    }
}
