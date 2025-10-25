using System.Collections.Generic;
using AnimalZoo.App.Interfaces;
using AnimalZoo.App.Localization;

namespace AnimalZoo.App.Models
{
    /// <summary>
    /// Penguin: non-flyable, cheerful slider.
    /// Crazy action: performs an ice slide, becomes Happy, returns a localized line.
    /// </summary>
    public sealed class Penguin : Animal, ICrazyAction
    {
        public Penguin(string name, double age) : base(name, age) { }
        public Penguin(string name, int age) : base(name, age) { }

        /// <summary>Short animal voice text used in logs; audio is played by the VM via SoundService.</summary>
        public override string MakeSound() => "Honk!";

        /// <summary>Localized textual description.</summary>
        public override string Describe() => string.Format(Loc.Instance["Penguin.Describe"], Name, Age);

        /// <summary>
        /// Slides playfully: switches mood to Happy and returns a localized log/alert line.
        /// </summary>
        public string ActCrazy(List<Animal> allAnimals)
        {
            SetMood(AnimalMood.Happy);
            return string.Format(Loc.Instance["Penguin.Crazy.Slide"], Name);
        }

        public override string OnNeighborJoined(Animal newcomer)
            => string.Format(Loc.Instance["Penguin.Neighbor"], Name, newcomer.Name);
    }
}