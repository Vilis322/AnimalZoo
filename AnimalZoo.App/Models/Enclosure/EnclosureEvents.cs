using System;
using System.Collections.Generic;

namespace AnimalZoo.App.Models;

/// <summary>Event args for when a new animal joins the same enclosure.</summary>
public sealed class AnimalJoinedEventArgs : EventArgs
{
    public Animal Newcomer { get; }
    public IReadOnlyList<Animal> CurrentResidents { get; }

    public AnimalJoinedEventArgs(Animal newcomer, IReadOnlyList<Animal> currentResidents)
    {
        Newcomer = newcomer;
        CurrentResidents = currentResidents;
    }
}

/// <summary>Event args for when food is dropped into the enclosure.</summary>
public sealed class FoodDroppedEventArgs : EventArgs
{
    public DateTime When { get; }

    public FoodDroppedEventArgs(DateTime when) => When = when;
}
