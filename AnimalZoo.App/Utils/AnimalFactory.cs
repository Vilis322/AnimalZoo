using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AnimalZoo.App.Models;

namespace AnimalZoo.App.Utils;

/// <summary>
/// UI option for selecting an Animal type.
/// </summary>
public sealed class AnimalTypeOption
{
    public string DisplayName { get; }
    public Type UnderlyingType { get; }

    public AnimalTypeOption(string displayName, Type type)
    {
        DisplayName = displayName;
        UnderlyingType = type;
    }

    public override string ToString() => DisplayName;
}

/// <summary>
/// Provides reflection-based discovery of Animal subclasses + factory.
/// Ensures future Animal classes appear automatically in the Type dropdown.
/// </summary>
public static class AnimalFactory
{
    /// <summary>Get list of all non-abstract subclasses of Animal in current assembly.</summary>
    public static List<AnimalTypeOption> GetAvailableAnimalTypes()
    {
        var asm = Assembly.GetExecutingAssembly();

        var types = asm.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.IsSubclassOf(typeof(Animal)))
            .OrderBy(t => t.Name)
            .Select(t => new AnimalTypeOption(ToDisplayName(t), t))
            .ToList();

        return types;
    }

    private static string ToDisplayName(Type t) => t.Name;

    /// <summary>
    /// Create Animal instance by best-matching ctor:
    /// prefers (string name, double age); falls back to (string name, int age) if necessary.
    /// </summary>
    public static Animal? Create(Type? animalType, string name, double age)
    {
        if (animalType is null) return null;

        // Prefer (string, double)
        var ctorDouble = animalType.GetConstructor(new[] { typeof(string), typeof(double) });
        if (ctorDouble is not null)
            return ctorDouble.Invoke(new object[] { name, age }) as Animal;

        // Fallback to (string, int) if class does not yet support double
        var ctorInt = animalType.GetConstructor(new[] { typeof(string), typeof(int) });
        if (ctorInt is not null)
        {
            // Round towards nearest int; this preserves reasonable semantics for older animals.
            var rounded = (int)Math.Round(age, MidpointRounding.AwayFromZero);
            return ctorInt.Invoke(new object[] { name, rounded }) as Animal;
        }

        return null;
    }
}
