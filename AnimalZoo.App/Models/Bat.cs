using System;
using System.Collections.Generic;
using AnimalZoo.App.Interfaces;
using AnimalZoo.App.Localization;

namespace AnimalZoo.App.Models
{
    /// <summary>
    /// Bat: a flyable nocturnal animal.
    /// Crazy action: uses echolocation and toggles flight (take off / land).
    /// </summary>
    public sealed class Bat : Animal, Flyable, ICrazyAction
    {
        /// <summary>Indicates whether the bat is airborne.</summary>
        public bool IsFlying { get; private set; }

        public Bat(string name, double age) : base(name, age) { }
        public Bat(string name, int age) : base(name, age) { }

        /// <summary>Short animal voice text used in logs; audio is played by the VM via SoundService.</summary>
        public override string MakeSound() => "Screech!";

        /// <summary>Localized textual description.</summary>
        public override string Describe() => string.Format(Loc.Instance["Bat.Describe"], Name, Age);

        /// <summary>
        /// Displayed state combines base DisplayState and a localized flying status.
        /// </summary>
        public override string DisplayState
            => $"{base.DisplayState} • {AnimalText.FlyingState(IsFlying)}";

        /// <summary>
        /// Toggles flight unless sleeping; raises DisplayState change notification.
        /// </summary>
        public void Fly()
        {
            if (Mood == AnimalMood.Sleeping) return;
            IsFlying = !IsFlying;
            base.OnPropertyChanged(nameof(DisplayState));
        }

        /// <summary>
        /// Crazy action: emit echolocation and toggle flight;
        /// returns a localized log/alert string.
        /// </summary>
        public string ActCrazy(List<Animal> allAnimals)
        {
            // Toggle flight state via Fly() to keep notifications consistent
            Fly();
            var key = IsFlying ? "Bat.Crazy.Echo.TakeOff" : "Bat.Crazy.Echo.Land";
            return string.Format(Loc.Instance[key], Name);
        }

        /// <summary>
        /// If bat falls asleep, it lands automatically and updates DisplayState.
        /// </summary>
        protected override void OnMoodChanged(AnimalMood newMood)
        {
            if (newMood == AnimalMood.Sleeping && IsFlying)
            {
                IsFlying = false;
                base.OnPropertyChanged(nameof(DisplayState));
            }
        }

        public override string OnNeighborJoined(Animal newcomer)
            => string.Format(Loc.Instance["Bat.Neighbor"], Name, newcomer.Name);
    }
}