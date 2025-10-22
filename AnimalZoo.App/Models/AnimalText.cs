using System;
using AnimalZoo.App.Localization;

namespace AnimalZoo.App.Models
{
    /// <summary>
    /// Helper methods to translate domain values (moods, type names, flying state, units).
    /// Keeps model/UI code clean and centralizes i18n keys.
    /// </summary>
    public static class AnimalText
    {
        /// <summary>Returns localized mood text (e.g., "Hungry").</summary>
        public static string Mood(AnimalMood mood)
            => mood switch
            {
                AnimalMood.Hungry   => Loc.Instance["Mood.Hungry"],
                AnimalMood.Happy    => Loc.Instance["Mood.Happy"],
                AnimalMood.Sleeping => Loc.Instance["Mood.Sleeping"],
                AnimalMood.Gaming   => Loc.Instance["Mood.Gaming"],
                _ => mood.ToString()
            };

        /// <summary>Returns localized flying/perched state.</summary>
        public static string FlyingState(bool isFlying)
            => isFlying ? Loc.Instance["FlyingState.Flying"] : Loc.Instance["FlyingState.Perched"];

        /// <summary>Returns localized type name using the CLR type name (e.g., "Cat").</summary>
        public static string TypeName(Type t)
            => Loc.Instance[$"AnimalType.{t.Name}"];

        /// <summary>Returns a short localized suffix for years ("y.o.", "г.", "a.").</summary>
        public static string YearsShort() => Loc.Instance["Units.YearsShort"];
    }
}