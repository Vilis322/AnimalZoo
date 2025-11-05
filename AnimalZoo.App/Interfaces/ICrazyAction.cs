using System.Collections.Generic;
using AnimalZoo.App.Models;

namespace AnimalZoo.App.Interfaces;

/// <summary>
/// Interface describing a "crazy" action that an animal can perform.
/// </summary>
public interface ICrazyAction
{
    /// <summary>
    /// Perform a crazy action possibly involving other animals.
    /// Returns a localizable message (key + parameters) or null if unable to perform.
    /// </summary>
    /// <param name="allAnimals">All animals currently present.</param>
    NeighborReaction? ActCrazy(List<Animal> allAnimals);
}
