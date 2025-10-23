using System.Threading.Tasks;
using AnimalZoo.App.Interfaces;
using AnimalZoo.App.Localization;
using AnimalZoo.App.Utils;

namespace AnimalZoo.App.Models;

/// <summary>
/// Dog animal. During crazy action, in addition to the localized log line,
/// it plays a dedicated "crazy_action.wav" sound from Assets/Dog/.
/// </summary>
public sealed class Dog : Animal, ICrazyAction
{
    public Dog(string name, double age) : base(name, age) { }
    public Dog(string name, int age)    : base(name, age) { }

    public override string MakeSound() => "Woof!";

    /// <summary>
    /// Crazy action: produces a localized log string and plays a special effect sound.
    /// </summary>
    public string ActCrazy(System.Collections.Generic.List<Animal> allAnimals)
    {
        // Fire-and-forget effect playback; do not block UI thread
        _ = PlayCrazyEffectAsync();

        // Keep the original localized message
        return string.Format(Loc.Instance["Dog.Crazy.MultiWoof"], Name);
    }

    /// <summary>
    /// Plays the dedicated "crazy_action.wav" from Assets/Dog/ via SoundService.
    /// </summary>
    private static async Task PlayCrazyEffectAsync()
    {
        try
        {
            await SoundService.PlayAnimalEffectAsync("Dog", "crazy_action.wav");
        }
        catch
        {
            // Intentionally ignore sound errors; logging/alerts not required here.
        }
    }

    public override string OnNeighborJoined(Animal newcomer)
        => string.Format(Loc.Instance["Dog.Neighbor"], Name, newcomer.Name);
}