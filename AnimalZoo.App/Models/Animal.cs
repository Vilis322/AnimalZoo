using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AnimalZoo.App.Models;

/// <summary>
/// Abstract base class for all animals.
/// Implements INotifyPropertyChanged so UI updates on property changes.
/// Provides a virtual DisplayState and a mood-change hook for derived types.
/// </summary>
public abstract class Animal : INotifyPropertyChanged
{
    private string _name;
    private double _age; // Fractional ages are supported (e.g., 2.6 years)
    private AnimalMood _mood = AnimalMood.Hungry;

    /// <summary>
    /// Stable unique identifier generated on construction.
    /// Kept short to be user-friendly in lists.
    /// </summary>
    public string UniqueId { get; } = Guid.NewGuid().ToString("N")[..8];

    /// <summary>Name of the animal.</summary>
    public string Name
    {
        get => _name;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
                return;

            if (_name != value)
            {
                _name = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(DisplayState));
                OnPropertyChanged(nameof(Identifier));
            }
        }
    }

    /// <summary>Age of the animal in years (supports fractional values, e.g., 2.6).</summary>
    public double Age
    {
        get => _age;
        set
        {
            var normalized = value < 0 ? 0 : value; // clamp negatives
            if (Math.Abs(_age - normalized) > double.Epsilon)
            {
                _age = normalized;
                OnPropertyChanged();
                OnPropertyChanged(nameof(DisplayState));
            }
        }
    }

    /// <summary>Current mood (state machine).</summary>
    public AnimalMood Mood
    {
        get => _mood;
        private set
        {
            if (_mood != value)
            {
                _mood = value;

                OnPropertyChanged();
                OnPropertyChanged(nameof(DisplayState));

                OnMoodChanged(_mood);
            }
        }
    }

    /// <summary>
    /// Human-readable display of current state (localized mood).
    /// </summary>
    public virtual string DisplayState => $"{Name} • {AnimalText.Mood(Mood)}";

    /// <summary>
    /// Identifier string shown to users: "Name-Type - ID".
    /// </summary>
    public string Identifier => $"{Name}-{GetType().Name} - {UniqueId}";

    /// <summary>
    /// Base constructor. Uses property setters for consistent validation/notifications.
    /// </summary>
    protected Animal(string name, double age)
    {
        _name = "Unnamed";
        _age = 0;

        Name = string.IsNullOrWhiteSpace(name) ? "Unnamed" : name;
        Age = age;
    }

    /// <summary>Explicit mood setter used by the higher-level state machine (VM).</summary>
    public void SetMood(AnimalMood mood) => Mood = mood;

    /// <summary>Hook called every time the mood changes.</summary>
    protected virtual void OnMoodChanged(AnimalMood newMood) { }

    /// <summary>Optional hook for enclosure neighbor join event. Override to customize.</summary>
    public virtual string OnNeighborJoined(Animal newcomer)
        => $"{Name} notices {newcomer.Name}.";

    /// <summary>Virtual describe method.</summary>
    public virtual string Describe()
        => $"{Name} is a {GetType().Name} aged {Age}.";

    /// <summary>Produce the animal sound.</summary>
    public abstract string MakeSound();

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? prop = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
}
