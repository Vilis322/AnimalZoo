using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using AnimalZoo.App.Localization;
using AnimalZoo.App.Models;

namespace AnimalZoo.App.Utils;

/// <summary>
/// UI option for selecting an Animal type.
/// DisplayName is localized at runtime based on the current language,
/// while Key is a stable identifier equal to the underlying class name (e.g., "Cat", "Dog").
/// </summary>
public sealed class AnimalTypeOption : INotifyPropertyChanged
{
    private readonly ILocalizationService _loc = Loc.Instance;

    /// <summary>
    /// Stable key used for localization lookup and any internal mapping.
    /// Equals the CLR type name of the animal class (e.g., "Cat", "Dog").
    /// </summary>
    public string Key { get; }

    /// <summary>
    /// Underlying concrete animal type (non-abstract subclass of Animal).
    /// </summary>
    public Type UnderlyingType { get; }

    /// <summary>
    /// Localized display name resolved from i18n resources at "AnimalType.{Key}".
    /// When language changes, the property raises change notifications automatically.
    /// </summary>
    public string DisplayName => _loc[$"AnimalType.{Key}"];

    public AnimalTypeOption(string key, Type type)
    {
        Key = key ?? throw new ArgumentNullException(nameof(key));
        UnderlyingType = type ?? throw new ArgumentNullException(nameof(type));

        // Subscribe to language change to refresh DisplayName binding.
        _loc.LanguageChanged += () => OnPropertyChanged(nameof(DisplayName));
    }

    public override string ToString() => DisplayName;

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? prop = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
}

/// <summary>
/// Provides reflection-based discovery of Animal subclasses + factory.
/// Ensures future Animal classes appear automatically in the Type dropdown.
/// </summary>
public static class AnimalFactory
{
    /// <summary>
    /// Gets a list of all non-abstract subclasses of Animal in the current assembly.
    /// Each item exposes a stable Key (type name) and a localized DisplayName.
    /// </summary>
    public static List<AnimalTypeOption> GetAvailableAnimalTypes()
    {
        var asm = Assembly.GetExecutingAssembly();

        var types = asm.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.IsSubclassOf(typeof(Animal)))
            .OrderBy(t => t.Name)
            .Select(t => new AnimalTypeOption(t.Name, t))
            .ToList();

        return types;
    }

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

