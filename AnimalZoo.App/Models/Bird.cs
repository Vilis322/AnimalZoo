using System.Collections.Generic;
using AnimalZoo.App.Interfaces;

namespace AnimalZoo.App.Models;

/// <summary>
/// Bird animal. Implements Flyable; crazy action toggles flying and shouts.
/// </summary>
public sealed class Bird : Animal, Flyable, ICrazyAction
{
    /// <summary>Whether the bird is currently flying.</summary>
    public bool IsFlying { get; private set; }

    /// <summary>Show both mood and flying state.</summary>
    public override string DisplayState
        => $"{base.DisplayState} and {(IsFlying ? "Flying" : "Perched")}";

    public Bird(string name, int age) : base(name, age)
    {
        IsFlying = false;
    }

    public override string MakeSound() => "Chirp!";

    /// <summary>
    /// Toggle flight only if not sleeping.
    /// </summary>
    public void ToggleFly()
    {
        if (Mood == AnimalMood.Sleeping)
        {
            // Cannot change flight while sleeping
            return;
        }

        IsFlying = !IsFlying;
        // DisplayState depends on IsFlying
        base.OnPropertyChanged(nameof(DisplayState));
    }

    public void Fly() => ToggleFly();

    /// <summary>
    /// If sleeping, do nothing; otherwise toggle flight with shout.
    /// </summary>
    public string ActCrazy(List<Animal> allAnimals)
    {
        if (Mood == AnimalMood.Sleeping)
        {
            return $"{Name} is sleeping and cannot fly now.";
        }

        ToggleFly();
        return IsFlying
            ? $"{Name} is now FLYING and screams 'CHIRP!!!'"
            : $"{Name} lands gracefully.";
    }

    /// <summary>
    /// If mood becomes Sleeping, force landing.
    /// </summary>
    protected override void OnMoodChanged(AnimalMood newMood)
    {
        if (newMood == AnimalMood.Sleeping && IsFlying)
        {
            IsFlying = false;
            base.OnPropertyChanged(nameof(DisplayState));
        }
    }
}