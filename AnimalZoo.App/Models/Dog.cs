using System.Linq;
using AnimalZoo.App.Interfaces;

namespace AnimalZoo.App.Models;

/// <summary>
/// Dog animal. Crazy action: barks 5 times like a mini-performance.
/// </summary>
public sealed class Dog : Animal, ICrazyAction
{
    public Dog(string name, int age) : base(name, age) { }

    /// <inheritdoc />
    public override string MakeSound() => "Woof!";

    /// <inheritdoc />
    public string ActCrazy(System.Collections.Generic.List<Animal> allAnimals)
    {
        var times = 5;
        var s = string.Join(" ", Enumerable.Repeat("WOOF!", times));
        return $"{Name} barks: {s}";
    }
}
