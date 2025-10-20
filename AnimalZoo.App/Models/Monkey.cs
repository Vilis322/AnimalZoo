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
    /// Swap names of two random animals and report the actual names that were swapped.
    /// </summary>
    public string ActCrazy(List<Animal> allAnimals)
    {
        if (allAnimals is null || allAnimals.Count < 2)
            return $"{Name} wanted to swap names, but not enough animals around.";

        var rnd = new Random();
        var a = allAnimals[rnd.Next(allAnimals.Count)];
        Animal b;
        do { b = allAnimals[rnd.Next(allAnimals.Count)]; } while (ReferenceEquals(a, b));

        // Capture original names for the message
        var originalA = a.Name;
        var originalB = b.Name;

        // Perform the swap
        (a.Name, b.Name) = (b.Name, a.Name);

        // Return message with instance names 
        return $"{Name} swapped names of a {originalA} ({a.GetType().Name}) and {originalB} ({b.GetType().Name}). Chaos!";
    }

    /// <summary>
    /// Reaction to a newcomer in the same enclosure.
    /// </summary>
    public override string OnNeighborJoined(Animal newcomer)
        => $"{Name} squeals happily and throws a peanut to {newcomer.Name}.";
}