using System.Collections.Generic;
using AnimalZoo.App.Interfaces;
using AnimalZoo.App.Localization;

namespace AnimalZoo.App.Models;

/// <summary>Turtle: very slow but determined; crazy action announces a tiny "race".</summary>
public sealed class Turtle : Animal, ICrazyAction
{
    public Turtle(string name, double age) : base(name, age) { }
    public Turtle(string name, int age)    : base(name, age) { }

    public override string MakeSound() => "..."; // quiet turtle

    public override string Describe() => string.Format(Loc.Instance["Turtle.Describe"], Name, Age);

    public string ActCrazy(List<Animal> allAnimals)
        => string.Format(Loc.Instance["Turtle.Crazy.Race"], Name);

    public override NeighborReaction? OnNeighborJoined(Animal newcomer)
        => new NeighborReaction("Turtle.Neighbor");
}