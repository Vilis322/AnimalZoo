using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AnimalZoo.App.Localization;

namespace AnimalZoo.App.Models;

/// <summary>
/// Generic enclosure (pen) for animals.
/// Holds residents, raises events (animal joined, food dropped),
/// and can orchestrate feeding with progress order.
/// </summary>
public sealed class Enclosure<T> where T : Animal
{
    private readonly List<T> _residents = new();

    /// <summary>Raised when a new animal joins and others are already inside.</summary>
    public event EventHandler<AnimalJoinedEventArgs>? AnimalJoinedInSameEnclosure;

    /// <summary>Raised when food is dropped.</summary>
    public event EventHandler<FoodDroppedEventArgs>? FoodDropped;

    /// <summary>Current residents.</summary>
    public IReadOnlyList<T> Residents => _residents;

    /// <summary>Add a resident and notify existing animals.</summary>
    public void Add(T animal)
    {
        if (_residents.Count > 0)
        {
            AnimalJoinedInSameEnclosure?.Invoke(
                this,
                new AnimalJoinedEventArgs(animal, _residents.Cast<Animal>().ToList())
            );
        }

        _residents.Add(animal);
    }

    /// <summary>Remove a resident.</summary>
    public bool Remove(T animal) => _residents.Remove(animal);

    /// <summary>
    /// Simulate dropping food: writes step-by-step progress and calls a per-animal callback when it "eats".
    /// </summary>
    /// <param name="log">Append log line.</param>
    /// <param name="onAte">Callback invoked for each animal as it finishes eating.</param>
    /// <param name="token">Cancellation token to abort the sequence.</param>
    public async Task DropFoodAsync(Action<string> log, Action<Animal>? onAte = null, CancellationToken token = default)
    {
        // Localizer is resolved once; keys are looked up at call time so current language is used.
        var loc = Loc.Instance;

        FoodDropped?.Invoke(this, new FoodDroppedEventArgs(DateTime.Now));

        var order = _residents.ToList();
        var rnd = new Random();
        order = order.OrderBy(_ => rnd.Next()).ToList();

        int step = 1;
        foreach (var a in order)
        {
            if (token.IsCancellationRequested) break;

            // Localized: "[{step}/{total}] {name} starts eating ..."
            log?.Invoke(string.Format(loc["Feeding.Start"], step, order.Count, a.Name));

            await Task.Delay(TimeSpan.FromMilliseconds(rnd.Next(700, 1400)), token);

            // Localized: "[{step}/{total}] {name} finished eating."
            log?.Invoke(string.Format(loc["Feeding.Finish"], step, order.Count, a.Name));

            onAte?.Invoke(a);
            step++;
        }
    }
}
