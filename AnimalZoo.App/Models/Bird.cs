using System.Collections.Generic;
using AnimalZoo.App.Interfaces;

namespace AnimalZoo.App.Models;

/// <summary>
/// Bird animal. Implements Flyable; cannot fly while sleeping.
/// Shows mood and flying state in DisplayState.
/// </summary>
public sealed class Bird : Animal, Flyable, ICrazyAction
{
    /// <summary>Whether the bird is currently flying.</summary>
    public bool IsFlying { get; private set; }

    /// <summary>Show both mood and flying state.</summary>
    public override string DisplayState
        => $"{base.DisplayState} • {(IsFlying ? "Flying" : "Perched")}";

    /// <summary>Create a Bird with fractional age support.</summary>
    public Bird(string name, double age) : base(name, age)
    {
        IsFlying = false;
    }

    /// <summary>Backward-compatible ctor (int age).</summary>
    public Bird(string name, int age) : base(name, age)
    {
        IsFlying = false;
    }

    public override string MakeSound() => "Chirp!";

    /// <summary>Toggle flight only if not sleeping.</summary>
    public void ToggleFly()
    {
        if (Mood == AnimalMood.Sleeping) return;
        IsFlying = !IsFlying;
        base.OnPropertyChanged(nameof(DisplayState));
    }

    public void Fly() => ToggleFly();

    public string ActCrazy(List<Animal> allAnimals)
    {
        if (Mood == AnimalMood.Sleeping)
            return $"{Name} is sleeping and cannot fly now.";

        ToggleFly();
        return IsFlying
            ? $"{Name} is now FLYING and screams 'CHIRP!!!'"
            : $"{Name} lands gracefully.";
    }

    /// <summary>Force landing when falling asleep.</summary>
    protected override void OnMoodChanged(AnimalMood newMood)
    {
        if (newMood == AnimalMood.Sleeping && IsFlying)
        {
            IsFlying = false;
            base.OnPropertyChanged(nameof(DisplayState));
        }
    }

    public override string OnNeighborJoined(Animal newcomer)
        => $"{Name} chirps at {newcomer.Name}.";
}
