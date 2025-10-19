using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using AnimalZoo.App.Utils;

namespace AnimalZoo.App.ViewModels;

/// <summary>
/// ViewModel for AddAnimalWindow: holds input and list of available types.
/// </summary>
public sealed class AddAnimalViewModel : INotifyPropertyChanged
{
    private string _name = string.Empty;
    private double _age;
    private AnimalTypeOption? _selectedType;

    /// <summary>Animal name.</summary>
    public string Name
    {
        get => _name;
        set { if (_name != value) { _name = value; OnPropertyChanged(); } }
    }

    /// <summary>Animal age (NumericUpDown uses double).</summary>
    public double Age
    {
        get => _age;
        set { if (Math.Abs(_age - value) > double.Epsilon) { _age = value; OnPropertyChanged(); } }
    }

    /// <summary>Available animal types discovered via reflection.</summary>
    public ObservableCollection<AnimalTypeOption> AnimalTypes { get; } = new();

    /// <summary>Currently selected type option.</summary>
    public AnimalTypeOption? SelectedType
    {
        get => _selectedType;
        set { if (!Equals(_selectedType, value)) { _selectedType = value; OnPropertyChanged(); } }
    }

    public AddAnimalViewModel()
    {
        var types = AnimalFactory.GetAvailableAnimalTypes();
        foreach (var t in types)
            AnimalTypes.Add(t);

        SelectedType = AnimalTypes.FirstOrDefault();
        Age = 1;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? prop = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
}