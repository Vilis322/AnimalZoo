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
    private double _age;                    // Fractional ages are supported (e.g., 2.6 years)
    private AnimalMood _mood = AnimalMood.Hungry;

    /// <summary>Name of the animal.</summary>
    public string Name
    {
        get => _name;
        set
        {
            // Soft validation: disallow empty/whitespace names.
            // Invalid assignment is ignored to avoid breaking UI flows.
            if (string.IsNullOrWhiteSpace(value))
                return;

            if (_name != value)
            {
                _name = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(DisplayState));
            }
        }
    }

    /// <summary>Age of the animal in years (supports fractional values, e.g., 2.6).</summary>
    public double Age
    {
        get => _age;
        set
        {
            // Clamp to non-negative range; fractional ages are allowed.
            var normalized = value < 0 ? 0 : value;
            if (_age != normalized)
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

                // Notify state-related bindings first…
                OnPropertyChanged();
                OnPropertyChanged(nameof(DisplayState));

                // …then allow derived classes to react (e.g., force landing when sleeping).
                OnMoodChanged(_mood);
            }
        }
    }

    /// <summary>
    /// Human-readable display of current state.
    /// Marked virtual so derived types (e.g., Bird/Eagle/Parrot) can append flying state, etc.
    /// </summary>
    public virtual string DisplayState => $"{GetType().Name} • {Name} • {Mood}";

    /// <summary>
    /// Base constructor. Uses property setters for consistent validation/notifications.
    /// </summary>
    protected Animal(string name, double age)
    {
        // Initialize backing fields to safe defaults before using property setters.
        _name = "Unnamed";
        _age = 0;

        Name = string.IsNullOrWhiteSpace(name) ? "Unnamed" : name;
        Age = age;
    }

    /// <summary>
    /// Explicit mood setter used by the higher-level state machine (VM).
    /// </summary>
    public void SetMood(AnimalMood mood) => Mood = mood;

    /// <summary>
    /// Hook called every time the mood changes.
    /// Derived classes may override to enforce additional rules (e.g., auto-land on sleep).
    /// </summary>
    /// <param name="newMood">The new mood set on this animal.</param>
    protected virtual void OnMoodChanged(AnimalMood newMood)
    {
        // Default: do nothing.
        // Derived classes (Bird/Eagle/Parrot) override this to update their own state.
    }

    /// <summary>
    /// Optional hook for enclosure neighbor join event. Override to customize.
    /// </summary>
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
