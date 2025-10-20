using AnimalZoo.App.Interfaces;

namespace AnimalZoo.App.Models;

/// <summary>
/// Dog animal.
/// </summary>
public sealed class Dog : Animal, ICrazyAction
{
    public Dog(string name, int age) : base(name, age) { }

    public override string MakeSound() => "Woof!";

    public string ActCrazy(System.Collections.Generic.List<Animal> allAnimals)
    {
        // barks 5 times
        return $"{Name}: " + string.Concat(System.Linq.Enumerable.Repeat("Woof! ", 5)).TrimEnd();
    }

    public override string OnNeighborJoined(Animal newcomer)
        => $"{Name} happily wags tail at {newcomer.Name}.";
}