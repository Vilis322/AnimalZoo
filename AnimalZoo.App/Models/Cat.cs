using System;
using System.Collections.Generic;
using AnimalZoo.App.Interfaces;

namespace AnimalZoo.App.Models;

/// <summary>
/// Cat animal. Crazy action: steals food from the kitchen (flavor text).
/// </summary>
public sealed class Cat : Animal, ICrazyAction
{
    public Cat(string name, int age) : base(name, age) { }

    /// <inheritdoc />
    public override string MakeSound() => "Meow!";

    /// <inheritdoc />
    public string ActCrazy(List<Animal> allAnimals)
    {
        var items = new[] { "cheese", "sausage", "fish", "butter", "yogurt" };
        var rand = new Random();
        var item = items[rand.Next(items.Length)];
        return $"{Name} stole {item} from the kitchen!";
    }
}
