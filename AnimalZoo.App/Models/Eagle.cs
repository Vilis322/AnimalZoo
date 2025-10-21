using System.Collections.Generic;
using AnimalZoo.App.Interfaces;

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
    public override string DisplayState => $"{base.DisplayState} • {(IsFlying ? "Flying" : "Perched")}";

    /// <summary>Create an Eagle with fractional age support.</summary>
    public Eagle(string name, double age) : base(name, age)
    {
        IsFlying = false;
    }

    /// <summary>Backward-compatible ctor (int age).</summary>
    public Eagle(string name, int age) : base(name, age)
    {
        IsFlying = false;
    }

    /// <inheritdoc />
    public override string MakeSound() => "Screech!";

    /// <summary>
    /// Toggle flight (disabled while sleeping).
    /// </summary>
    public void ToggleFly()
    {
        if (Mood == AnimalMood.Sleeping) return;
        IsFlying = !IsFlying;
        // notify DisplayState changed
        base.OnPropertyChanged(nameof(DisplayState));
    }

    /// <inheritdoc />
    public void Fly() => ToggleFly();

    /// <summary>
    /// Crazy action: perform a dramatic takeoff/landing unless sleeping.
    /// </summary>
    public string ActCrazy(List<Animal> allAnimals)
    {
        if (Mood == AnimalMood.Sleeping)
            return $"{Name} is sleeping and cannot fly now.";

        ToggleFly();
        return IsFlying
            ? $"{Name} soars into the sky with a mighty screech!"
            : $"{Name} folds its wings and perches.";
    }

    /// <summary>
    /// Land automatically when going to Sleeping.
    /// </summary>
    protected override void OnMoodChanged(AnimalMood newMood)
    {
        if (newMood == AnimalMood.Sleeping && IsFlying)
        {
            IsFlying = false;
            base.OnPropertyChanged(nameof(DisplayState));
        }
    }

    /// <summary>
    /// Reaction to a newcomer in the same enclosure.
    /// </summary>
    public override string OnNeighborJoined(Animal newcomer)
    {
        // Slightly different reaction to other birds vs other animals.
        return newcomer is Bird
            ? $"{Name} spreads wings and asserts dominance over {newcomer.Name}."
            : $"{Name} watches {newcomer.Name} from above with a piercing look.";
    }
}
