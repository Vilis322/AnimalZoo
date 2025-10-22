using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using AnimalZoo.App.Localization;
using AnimalZoo.App.Utils;

namespace AnimalZoo.App.ViewModels
{
    /// <summary>
    /// ViewModel for AddAnimalWindow: holds user input and type list.
    /// Provides culture-agnostic age parsing if needed, but currently NumericUpDown binds directly to Age.
    /// Exposes localized label texts for the dialog UI.
    /// </summary>
    public sealed class AddAnimalViewModel : INotifyPropertyChanged
    {
        private readonly ILocalizationService _loc = Loc.Instance;

        private string _name = string.Empty;
        private double _age = 1.0;
        private string _ageInput = "1"; // Kept for future text-based input, not used in current NumericUpDown binding.
        private AnimalTypeOption? _selectedType;

        public ObservableCollection<AnimalTypeOption> AnimalTypes { get; } = new();

        // === Localized UI text (bind from XAML) ===
        public string TextDialogTitle   => _loc["Dialog.AddAnimal.Title"];
        public string TextCreateHeading => _loc["Dialog.AddAnimal.Heading"];
        public string TextNameLabel     => _loc["Labels.Name"];
        public string TextAgeLabel      => _loc["Labels.Age"];
        public string TextTypeLabel     => _loc["Labels.Type"];
        public string TextOk            => _loc["Buttons.OK"];
        public string TextCancel        => _loc["Buttons.Cancel"];

        /// <summary>
        /// Localized alert text used by dialog validation when name is empty.
        /// Code-behind can bind/read this string instead of hardcoding the message.
        /// </summary>
        public string TextNameMissingAlert => _loc["Alerts.NameMissing"];

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
        /// Numeric age in years (can be fractional). Public setter is required for TwoWay binding with NumericUpDown.
        /// </summary>
        public double Age
        {
            get => _age;
            set
            {
                if (value < 0)
                    return;

                if (Math.Abs(_age - value) > double.Epsilon)
                {
                    _age = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>Text field for potential text-based age entry (not used by current XAML).</summary>
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
            AgeInput = "1";

            // Refresh labels when language changes
            _loc.LanguageChanged += () =>
            {
                OnPropertyChanged(nameof(TextDialogTitle));
                OnPropertyChanged(nameof(TextCreateHeading));
                OnPropertyChanged(nameof(TextNameLabel));
                OnPropertyChanged(nameof(TextAgeLabel));
                OnPropertyChanged(nameof(TextTypeLabel));
                OnPropertyChanged(nameof(TextOk));
                OnPropertyChanged(nameof(TextCancel));
                OnPropertyChanged(nameof(TextNameMissingAlert));
            };
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? prop = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
    }
}
