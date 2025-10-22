using AnimalZoo.App.Interfaces;
using AnimalZoo.App.Localization;

namespace AnimalZoo.App.Models;

/// <summary>Cat animal.</summary>
public sealed class Cat : Animal, ICrazyAction
{
    public Cat(string name, double age) : base(name, age) { }
    public Cat(string name, int age) : base(name, age) { }

    public override string MakeSound() => "Meow!";

    public string ActCrazy(System.Collections.Generic.List<Animal> allAnimals)
        => string.Format(Loc.Instance["Cat.Crazy.StealFood"], Name);

    public override string OnNeighborJoined(Animal newcomer)
        => string.Format(Loc.Instance["Cat.Neighbor"], Name, newcomer.Name);
}