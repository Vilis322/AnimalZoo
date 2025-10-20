using AnimalZoo.App.Interfaces;

namespace AnimalZoo.App.Models;

/// <summary>
/// Cat animal.
/// </summary>
public sealed class Cat : Animal, ICrazyAction
{
    public Cat(string name, int age) : base(name, age) { }

    public override string MakeSound() => "Meow!";

    public string ActCrazy(System.Collections.Generic.List<Animal> allAnimals)
    {
        // steals food as a crazy action
        return $"{Name} stealthily stole some food from the kitchen!";
    }

    public override string OnNeighborJoined(Animal newcomer)
        => $"{Name} hisses at {newcomer.Name}!";
}

