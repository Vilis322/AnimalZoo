using System;
using System.Collections.Generic;
using AnimalZoo.App.Models;

namespace AnimalZoo.App.Interfaces;

/// <summary>
/// Repository interface for animal persistence operations.
/// </summary>
public interface IAnimalsRepository
{
    /// <summary>Add or update an animal in the database.</summary>
    /// <param name="animal">The animal to save.</param>
    void Save(Animal animal);

    /// <summary>Delete an animal by its unique identifier.</summary>
    /// <param name="uniqueId">The unique identifier of the animal.</param>
    /// <returns>True if deleted, false if not found.</returns>
    bool Delete(string uniqueId);

    /// <summary>Retrieve an animal by its unique identifier.</summary>
    /// <param name="uniqueId">The unique identifier of the animal.</param>
    /// <returns>The animal if found, null otherwise.</returns>
    Animal? GetById(string uniqueId);

    /// <summary>Retrieve all animals from the database.</summary>
    /// <returns>Collection of all animals.</returns>
    IEnumerable<Animal> GetAll();

    /// <summary>Find animals by a predicate.</summary>
    /// <param name="predicate">The predicate to filter animals.</param>
    /// <returns>Filtered collection of animals.</returns>
    IEnumerable<Animal> Find(Func<Animal, bool> predicate);
}
