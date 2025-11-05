using System;
using System.Collections.Generic;
using System.Linq;
using AnimalZoo.App.Interfaces;
using AnimalZoo.App.Localization;

namespace AnimalZoo.App.Models;

/// <summary>Monkey: chaos action swaps names of two animals; reacts playfully to newcomers.</summary>
public sealed class Monkey : Animal, ICrazyAction
{
    public Monkey(string name, double age) : base(name, age) { }
    public Monkey(string name, int age)    : base(name, age) { }

    public override string MakeSound() => "Oo-oo-aa-aa!";
    public override string Describe() => string.Format(Loc.Instance["Monkey.Describe"], Name, Age);

    public NeighborReaction? ActCrazy(List<Animal> allAnimals)
    {
        if (allAnimals is null || allAnimals.Count < 2)
            return null; // Not enough animals to swap

        var rnd = new Random();
        var a = allAnimals[rnd.Next(allAnimals.Count)];
        Animal b;
        do { b = allAnimals[rnd.Next(allAnimals.Count)]; } while (ReferenceEquals(a, b));

        var originalA = a.Name;
        var originalB = b.Name;
        (a.Name, b.Name) = (b.Name, a.Name);

        // Localized message: actor + two names swapped
        return new NeighborReaction("Monkey.Crazy.Swap", Name, originalA, originalB);
    }

    public override NeighborReaction? OnNeighborJoined(Animal newcomer)
        => new NeighborReaction("Monkey.Neighbor");
}
