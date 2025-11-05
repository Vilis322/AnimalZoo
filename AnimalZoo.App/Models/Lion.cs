using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AnimalZoo.App.Interfaces;
using AnimalZoo.App.Localization;
using AnimalZoo.App.Utils;

namespace AnimalZoo.App.Models
{
    /// <summary>
    /// Lion: a non-flyable animal with a powerful roar.
    /// Crazy action: roars at a random other animal (which becomes Sleeping) and
    /// plays a dedicated "crazy_action.wav" sound from Assets/Lion/.
    /// </summary>
    public sealed class Lion : Animal, ICrazyAction
    {
        public Lion(string name, double age) : base(name, age) { }
        public Lion(string name, int age)    : base(name, age) { }

        /// <summary>Short animal voice text used in logs; audio for Make Sound is handled by the VM.</summary>
        public override string MakeSound() => "Roar!";

        /// <summary>Localized textual description.</summary>
        public override string Describe() => string.Format(Loc.Instance["Lion.Describe"], Name, Age);

        /// <summary>
        /// Crazy action: pick a random non-self animal, set it to Sleeping (startled),
        /// start playing the special crazy action sound, and return a localizable message.
        /// If no targets are available, returns a localized "no targets" message.
        /// </summary>
        public NeighborReaction? ActCrazy(List<Animal> allAnimals)
        {
            if (allAnimals is null || allAnimals.Count <= 1)
                return new NeighborReaction("Lion.Crazy.NoTargets", Name);

            var candidates = allAnimals.Where(a => !ReferenceEquals(a, this)).ToList();
            if (candidates.Count == 0)
                return new NeighborReaction("Lion.Crazy.NoTargets", Name);

            var target = candidates[new Random().Next(candidates.Count)];

            // Apply effect to the target
            target.SetMood(AnimalMood.Sleeping);

            // Fire-and-forget playback of dedicated crazy action sound
            _ = PlayCrazyEffectAsync();

            // Localized log/alert line with both names
            return new NeighborReaction("Lion.Crazy.Roar", Name, target.Name);
        }

        /// <summary>
        /// Plays the dedicated "crazy_action.wav" from Assets/Lion/ via SoundService.
        /// Errors are intentionally ignored to avoid disrupting UI flow.
        /// </summary>
        private static async Task PlayCrazyEffectAsync()
        {
            try
            {
                await SoundService.PlayAnimalEffectAsync("Lion", "crazy_action.wav");
            }
            catch
            {
                // Ignore playback errors silently.
            }
        }

        public override NeighborReaction? OnNeighborJoined(Animal newcomer)
            => new NeighborReaction("Lion.Neighbor", Name, newcomer.Name);
    }
}