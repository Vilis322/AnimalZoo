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
/// Ensures future Animal classes appear automatically in the Type dropdown,
/// assuming they are non-abstract and have ctor (string name, int age).
/// </summary>
public static class AnimalFactory
{
    /// <summary>
    /// Get list of all non-abstract subclasses of Animal in current assembly.
    /// </summary>
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
    /// Create Animal instance by (string name, int age) ctor.
    /// </summary>
    public static Animal? Create(Type? animalType, string name, int age)
    {
        if (animalType is null) return null;
        var ctor = animalType.GetConstructor(new[] { typeof(string), typeof(int) });
        if (ctor is null) return null;
        return ctor.Invoke(new object[] { name, age }) as Animal;
    }
}