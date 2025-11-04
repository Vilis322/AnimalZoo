using System.Collections.Generic;

namespace AnimalZoo.App.Interfaces;

/// <summary>
/// Repository interface for managing enclosure assignments.
/// Maps animals to their enclosures.
/// </summary>
public interface IEnclosureRepository
{
    /// <summary>Assign an animal to an enclosure.</summary>
    /// <param name="animalId">The unique identifier of the animal.</param>
    /// <param name="enclosureName">The name of the enclosure.</param>
    void AssignToEnclosure(string animalId, string enclosureName);

    /// <summary>Remove an animal from its enclosure.</summary>
    /// <param name="animalId">The unique identifier of the animal.</param>
    /// <returns>True if removed, false if not assigned.</returns>
    bool RemoveFromEnclosure(string animalId);

    /// <summary>Get the enclosure name for a specific animal.</summary>
    /// <param name="animalId">The unique identifier of the animal.</param>
    /// <returns>The enclosure name if assigned, null otherwise.</returns>
    string? GetEnclosureName(string animalId);

    /// <summary>Get all animal IDs assigned to a specific enclosure.</summary>
    /// <param name="enclosureName">The name of the enclosure.</param>
    /// <returns>Collection of animal unique identifiers.</returns>
    IEnumerable<string> GetAnimalsByEnclosure(string enclosureName);

    /// <summary>Get all enclosure names.</summary>
    /// <returns>Collection of unique enclosure names.</returns>
    IEnumerable<string> GetAllEnclosureNames();
}
