using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AnimalZoo.App.Models;

/// <summary>
/// Abstract base class for all animals.
/// Implements INotifyPropertyChanged so UI updates on property changes
/// (e.g., when names are swapped by Monkey's crazy action).
/// </summary>
public abstract class Animal : INotifyPropertyChanged
{
    private string _name;
    private int _age;
    private AnimalMood _mood = AnimalMood.Hungry;

    /// <summary>Name of the animal.</summary>
    public string Name
    {
        get => _name;
        set
        {
            if (_name != value)
            {
                _name = value;
                OnPropertyChanged(); // Name
            }
        }
    }

    /// <summary>Age in years.</summary>
    public int Age
    {
        get => _age;
        set
        {
            if (_age != value)
            {
                _age = value;
                OnPropertyChanged(); // Age
            }
        }
    }

    /// <summary>Current mood/state of the animal.</summary>
    public AnimalMood Mood
    {
        get => _mood;
        private set
        {
            if (_mood != value)
            {
                _mood = value;
                OnPropertyChanged(nameof(DisplayState));
                OnMoodChanged(_mood); // hook for subclasses
            }
        }
    }

    /// <summary>
    /// Display state for UI. Subclasses may extend (e.g., Bird shows flight).
    /// </summary>
    public virtual string DisplayState => Mood switch
    {
        AnimalMood.Hungry   => "Hungry",
        AnimalMood.Happy    => "Happy",
        AnimalMood.Sleeping => "Sleeping",
        AnimalMood.Gaming   => "Gaming",
        _ => "—"
    };

    protected Animal(string name, int age)
    {
        _name = name;
        _age = age;
        _mood = AnimalMood.Hungry;
    }

    /// <summary>
    /// Forcefully set mood (used by VM to drive the cycle).
    /// </summary>
    public void SetMood(AnimalMood mood)
    {
        if (_mood != mood)
        {
            Mood = mood; // setter will invoke OnMoodChanged + notify
        }
    }

    /// <summary>
    /// Subclasses can react to mood changes.
    /// </summary>
    /// <param name="newMood">New mood value.</param>
    protected virtual void OnMoodChanged(AnimalMood newMood) { }

    /// <summary>
    /// Virtual describe method; subclasses may override for extended info.
    /// </summary>
    public virtual string Describe()
        => $"{Name} is a {GetType().Name} aged {Age}.";

    /// <summary>
    /// Produce the animal sound.
    /// </summary>
    public abstract string MakeSound();

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? prop = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
}
