using System;
using System.Collections.Generic;
using System.Linq;
using AnimalZoo.App.Interfaces;

namespace AnimalZoo.App.Models;

/// <summary>Monkey: chaos action swaps names of two animals; reacts playfully to newcomers.</summary>
public sealed class Monkey : Animal, ICrazyAction
{
    public Monkey(string name, double age) : base(name, age) { }
    public Monkey(string name, int age)    : base(name, age) { }

    public override string MakeSound() => "Oo-oo-aa-aa!";
    public override string Describe() => $"{Name} is a playful Monkey aged {Age}.";

    public string ActCrazy(List<Animal> allAnimals)
    {
        if (allAnimals is null || allAnimals.Count < 2)
            return $"{Name} wanted to swap names, but not enough animals around.";

        var rnd = new Random();
        var a = allAnimals[rnd.Next(allAnimals.Count)];
        Animal b;
        do { b = allAnimals[rnd.Next(allAnimals.Count)]; } while (ReferenceEquals(a, b));

        var originalA = a.Name;
        var originalB = b.Name;
        (a.Name, b.Name) = (b.Name, a.Name);

        return $"{Name} swapped names of a {originalA} ({a.GetType().Name}) and {originalB} ({b.GetType().Name}). Chaos!";
    }

    public override string OnNeighborJoined(Animal newcomer)
        => $"{Name} squeals happily and throws a peanut to {newcomer.Name}.";
}
