using AnimalZoo.App.Interfaces;
using AnimalZoo.App.Localization;

namespace AnimalZoo.App.Models;

/// <summary>Dog animal.</summary>
public sealed class Dog : Animal, ICrazyAction
{
    public Dog(string name, double age) : base(name, age) { }
    public Dog(string name, int age)    : base(name, age) { }

    public override string MakeSound() => "Woof!";

    public string ActCrazy(System.Collections.Generic.List<Animal> allAnimals)
        => string.Format(Loc.Instance["Dog.Crazy.MultiWoof"], Name);

    public override string OnNeighborJoined(Animal newcomer)
        => string.Format(Loc.Instance["Dog.Neighbor"], Name, newcomer.Name);
}