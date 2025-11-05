using System.Collections.Generic;
using AnimalZoo.App.Interfaces;
using AnimalZoo.App.Localization;

namespace AnimalZoo.App.Models;

/// <summary>
/// Eagle: a flying predator; cannot fly while sleeping.
/// Shows mood and flying state in DisplayState. Implements Flyable and ICrazyAction.
/// </summary>
public sealed class Eagle : Animal, Flyable, ICrazyAction
{
    /// <summary>Whether eagle is currently flying.</summary>
    public bool IsFlying { get; private set; }

    /// <inheritdoc />
    public override string DisplayState => $"{base.DisplayState} • {AnimalText.FlyingState(IsFlying)}";

    /// <summary>Create an Eagle with fractional age support.</summary>
    public Eagle(string name, double age) : base(name, age) { IsFlying = false; }

    /// <summary>Backward-compatible ctor (int age).</summary>
    public Eagle(string name, int age) : base(name, age) { IsFlying = false; }

    /// <inheritdoc />
    public override string MakeSound() => "Screech!";

    /// <summary>Toggle flight (disabled while sleeping).</summary>
    public void ToggleFly()
    {
        if (Mood == AnimalMood.Sleeping) return;
        IsFlying = !IsFlying;
        base.OnPropertyChanged(nameof(DisplayState));
    }

    /// <inheritdoc />
    public void Fly() => ToggleFly();

    /// <summary>Crazy action: dramatic takeoff/landing unless sleeping.</summary>
    public string ActCrazy(List<Animal> allAnimals)
    {
        if (Mood == AnimalMood.Sleeping)
            return string.Format(Loc.Instance["Eagle.Crazy.SleepingNoFly"], Name);

        ToggleFly();
        return IsFlying
            ? string.Format(Loc.Instance["Eagle.Crazy.Flying"], Name)
            : string.Format(Loc.Instance["Eagle.Crazy.Landing"], Name);
    }

    /// <summary>Land automatically when going to Sleeping.</summary>
    protected override void OnMoodChanged(AnimalMood newMood)
    {
        if (newMood == AnimalMood.Sleeping && IsFlying)
        {
            IsFlying = false;
            base.OnPropertyChanged(nameof(DisplayState));
        }
    }

    /// <summary>Reaction to a newcomer in the same enclosure.</summary>
    public override NeighborReaction? OnNeighborJoined(Animal newcomer)
    {
        return newcomer is Bird
            ? new NeighborReaction("Eagle.Neighbor.Bird")
            : new NeighborReaction("Eagle.Neighbor.Other");
    }
}
