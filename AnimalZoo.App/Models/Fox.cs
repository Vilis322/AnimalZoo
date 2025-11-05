using System.Collections.Generic;
using System.Linq;
using AnimalZoo.App.Interfaces;
using AnimalZoo.App.Localization;

namespace AnimalZoo.App.Models;

/// <summary>
/// Fox: sly animal that "steals" a treat from a random hungry neighbor as a crazy action.
/// </summary>
public sealed class Fox : Animal, ICrazyAction
{
    public Fox(string name, double age) : base(name, age) { }
    public Fox(string name, int age)    : base(name, age) { }

    public override string MakeSound() => "Ring-ding-ding!";
    public override string Describe() => string.Format(Loc.Instance["Fox.Describe"], Name, Age);

    public NeighborReaction? ActCrazy(List<Animal> allAnimals)
    {
        if (allAnimals is null || allAnimals.Count == 0)
            return null; // No animals to steal from

        var candidates = allAnimals.Where(a => !ReferenceEquals(a, this) && a.Mood == AnimalMood.Hungry).ToList();
        if (candidates.Count == 0)
            return null; // No hungry animals to target

        var rnd = new System.Random();
        var victim = candidates[rnd.Next(candidates.Count)];
        return new NeighborReaction("Fox.Crazy.Steal", Name);
    }

    public override NeighborReaction? OnNeighborJoined(Animal newcomer)
        => new NeighborReaction("Fox.Neighbor");
}