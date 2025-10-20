using System;
using System.Collections.Generic;
using System.Linq;
using AnimalZoo.App.Interfaces;

namespace AnimalZoo.App.Models;

/// <summary>
/// Monkey: chaos action swaps names of two animals; reacts playfully to newcomers.
/// </summary>
public sealed class Monkey : Animal, ICrazyAction
{
    /// <summary>Create a Monkey.</summary>
    public Monkey(string name, int age) : base(name, age) { }

    /// <inheritdoc />
    public override string MakeSound() => "Oo-oo-aa-aa!";

    /// <inheritdoc />
    public override string Describe() => $"{Name} is a playful Monkey aged {Age}.";

    /// <summary>
    /// Swap names of two random animals if there are at least two.
    /// </summary>
    public string ActCrazy(List<Animal> allAnimals)
    {
        if (allAnimals is null || allAnimals.Count < 2)
            return $"{Name} wanted to swap names, but not enough animals around.";

        var rnd = new Random();
        var a = allAnimals[rnd.Next(allAnimals.Count)];
        Animal b;
        do { b = allAnimals[rnd.Next(allAnimals.Count)]; } while (ReferenceEquals(a, b));

        (a.Name, b.Name) = (b.Name, a.Name);
        return $"{Name} swapped the names of {a.GetType().Name} and {b.GetType().Name}! Chaos!";
    }

    /// <summary>
    /// Reaction to a newcomer in the same enclosure.
    /// </summary>
    public override string OnNeighborJoined(Animal newcomer)
        => $"{Name} squeals happily and throws a peanut to {newcomer.Name}.";
}