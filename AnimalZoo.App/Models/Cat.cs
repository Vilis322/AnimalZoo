using AnimalZoo.App.Interfaces;

namespace AnimalZoo.App.Models;

/// <summary>Cat animal.</summary>
public sealed class Cat : Animal, ICrazyAction
{
    // New double-precision age ctor
    public Cat(string name, double age) : base(name, age) { }
    // Backward-compatible int ctor
    public Cat(string name, int age) : base(name, age) { }

    public override string MakeSound() => "Meow!";

    public string ActCrazy(System.Collections.Generic.List<Animal> allAnimals)
        => $"{Name} stealthily stole some food from the kitchen!";

    public override string OnNeighborJoined(Animal newcomer)
        => $"{Name} hisses at {newcomer.Name}!";
}
