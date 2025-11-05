using System;
using System.Collections.Generic;
using System.Linq;
using AnimalZoo.App.Interfaces;
using AnimalZoo.App.Localization;
using AnimalZoo.App.Utils;

namespace AnimalZoo.App.Models
{
    /// <summary>
    /// Parrot: flying talker; crazy action mimics a random non-parrot animal's sound.
    /// If no suitable targets are available, returns a localized "no one to mimic" message.
    /// Also triggers audio playback of the mimicked animal's voice.
    /// </summary>
    public sealed class Parrot : Animal, Flyable, ICrazyAction
    {
        public bool IsFlying { get; private set; }

        public Parrot(string name, double age) : base(name, age) { }
        public Parrot(string name, int age) : base(name, age) { }

        public override string MakeSound() => "Squawk!";
        public override string Describe() => string.Format(Loc.Instance["Parrot.Describe"], Name, Age);

        public override string DisplayState => $"{base.DisplayState} • {AnimalText.FlyingState(IsFlying)}";

        /// <summary>
        /// Toggles the flight state (no-op if sleeping). Raises DisplayState change notifications.
        /// </summary>
        public void Fly()
        {
            if (Mood == AnimalMood.Sleeping) return;
            IsFlying = !IsFlying;
            base.OnPropertyChanged(nameof(DisplayState));
        }

        /// <summary>
        /// Crazy action: mimic the sound of a random non-parrot animal that is currently present.
        /// - Picks target from 'allAnimals' excluding self and other parrots.
        /// - Returns a localized message with this parrot's name and the target animal's name.
        /// - Additionally plays the target animal's 'voice.wav'.
        /// </summary>
        public string ActCrazy(List<Animal> allAnimals)
        {
            if (allAnimals is null || allAnimals.Count == 0)
                return Loc.Instance["Parrot.Crazy.NoTargets"];

            var candidates = allAnimals.Where(a => !ReferenceEquals(a, this) && a is not Parrot).ToList();
            if (candidates.Count == 0)
                return Loc.Instance["Parrot.Crazy.NoTargets"];

            var rnd = new Random();
            var target = candidates[rnd.Next(candidates.Count)];

            // Trigger audio playback of the target's "voice.wav" (fire-and-forget).
            _ = PlayMimicAsync(target);

            // Return localized log/alert text "{ParrotName} mimics {TargetName}!"
            return string.Format(Loc.Instance["Parrot.Crazy.Mimic"], Name, target.Name);
        }

        /// <summary>
        /// Plays the voice of the mimicked animal based on its runtime type name.
        /// Errors are ignored to keep UI responsive.
        /// </summary>
        private static async Task PlayMimicAsync(Animal target)
        {
            try
            {
                var typeName = target.GetType().Name;
                await SoundService.PlayAnimalVoiceAsync(typeName);
            }
            catch
            {
                // Ignore any playback failures silently.
            }
        }

        protected override void OnMoodChanged(AnimalMood newMood)
        {
            if (newMood == AnimalMood.Sleeping && IsFlying)
            {
                IsFlying = false;
                base.OnPropertyChanged(nameof(DisplayState));
            }
        }

        public override NeighborReaction? OnNeighborJoined(Animal newcomer)
            => new NeighborReaction("Parrot.Neighbor");
    }
}
