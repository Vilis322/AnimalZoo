using System.Collections.Generic;
using AnimalZoo.App.Interfaces;
using AnimalZoo.App.Localization;

namespace AnimalZoo.App.Models;

/// <summary>
/// Bird animal. Implements Flyable; cannot fly while sleeping.
/// Shows mood and flying state in DisplayState.
/// </summary>
public sealed class Bird : Animal, Flyable, ICrazyAction
{
    /// <summary>Whether the bird is currently flying.</summary>
    public bool IsFlying { get; private set; }

    /// <summary>Show both mood and flying state (localized).</summary>
    public override string DisplayState
        => $"{base.DisplayState} • {AnimalText.FlyingState(IsFlying)}";

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
            return string.Format(AnimalZoo.App.Localization.Loc.Instance["Bird.Crazy.SleepingNoFly"], Name);

        ToggleFly();
        return IsFlying
            ? string.Format(Loc.Instance["Bird.Crazy.Flying"], Name)
            : string.Format(Loc.Instance["Bird.Crazy.Landing"], Name);
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

    public override NeighborReaction? OnNeighborJoined(Animal newcomer)
        => new NeighborReaction("Bird.Neighbor");
}
