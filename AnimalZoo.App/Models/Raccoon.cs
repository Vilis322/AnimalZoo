using System;
using System.Collections.Generic;
using AnimalZoo.App.Interfaces;
using AnimalZoo.App.Localization;

namespace AnimalZoo.App.Models;

/// <summary>Raccoon: finds a shiny thing as a crazy action; reacts to neighbors curiously.</summary>
public sealed class Raccoon : Animal, ICrazyAction
{
    private static readonly string[] ShinyThings =
    {
        "a silver button", "a bottle cap", "a shiny coin", "a sparkling key",
        "a chrome bolt", "a glass marble", "a glittering ring"
    };

    public Raccoon(string name, double age) : base(name, age) { }
    public Raccoon(string name, int age)    : base(name, age) { }

    public override string MakeSound() => "Chrrr!";
    public override string Describe() => string.Format(Loc.Instance["Raccoon.Describe"], Name, Age);

    public string ActCrazy(List<Animal> allAnimals)
    {
        var rnd = new Random();
        var item = ShinyThings[rnd.Next(ShinyThings.Length)];
        return string.Format(Loc.Instance["Raccoon.Crazy.Found"], Name, item);
    }

    public override string OnNeighborJoined(Animal newcomer)
        => string.Format(Loc.Instance["Raccoon.Neighbor"], Name, newcomer.Name);
}