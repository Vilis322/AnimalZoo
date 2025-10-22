using System.Collections.Generic;
using System.Linq;
using AnimalZoo.App.Interfaces;
using AnimalZoo.App.Localization;

namespace AnimalZoo.App.Models
{
    /// <summary>
    /// Parrot: flying talker; crazy action mimics a non-parrot neighbor's sound.
    /// Implements Flyable (toggle via Fly()) and reacts to sleep by auto-landing.
    /// </summary>
    public sealed class Parrot : Animal, Flyable, ICrazyAction
    {
        public bool IsFlying { get; private set; }

        public Parrot(string name, double age) : base(name, age) { }
        public Parrot(string name, int age) : base(name, age) { }

        public override string MakeSound() => "Squawk!";
        public override string Describe() => string.Format(Loc.Instance["Parrot.Describe"], Name, Age);

        public override string DisplayState => $"{base.DisplayState} • {AnimalText.FlyingState(IsFlying)}";

        public void Fly()
        {
            if (Mood == AnimalMood.Sleeping) return;
            IsFlying = !IsFlying;
            base.OnPropertyChanged(nameof(DisplayState));
        }

        public string ActCrazy(List<Animal> allAnimals)
        {
            if (allAnimals is null || allAnimals.Count == 0)
                return string.Format(Loc.Instance["Parrot.Crazy.NoOne"], Name);

            var candidates = allAnimals.Where(a => !ReferenceEquals(a, this) && a is not Parrot).ToList();
            if (candidates.Count == 0)
                return string.Format(Loc.Instance["Parrot.Crazy.NoTarget"], Name);

            var rnd = new System.Random();
            var someone = candidates[rnd.Next(candidates.Count)];

            return string.Format(Loc.Instance["Parrot.Crazy.Mimic"], Name, someone.Name, someone.MakeSound());
        }

        protected override void OnMoodChanged(AnimalMood newMood)
        {
            if (newMood == AnimalMood.Sleeping && IsFlying)
            {
                IsFlying = false;
                base.OnPropertyChanged(nameof(DisplayState));
            }
        }
        
        public override string OnNeighborJoined(Animal newcomer)
            => string.Format(Loc.Instance["Parrot.Neighbor"], Name, newcomer.Name);
    }
}
