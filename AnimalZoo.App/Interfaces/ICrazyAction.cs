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
    /// Returns a human-readable log line.
    /// </summary>
    /// <param name="allAnimals">All animals currently present.</param>
    string ActCrazy(List<Animal> allAnimals);
}
