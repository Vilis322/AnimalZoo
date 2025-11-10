using System;
using System.Collections.Generic;
using System.Linq;
using AnimalZoo.App.Data;
using AnimalZoo.App.Interfaces;
using AnimalZoo.App.Models;
using Microsoft.EntityFrameworkCore;

namespace AnimalZoo.App.Repositories;

/// <summary>
/// Entity Framework Core implementation of the IAnimalsRepository interface.
/// Provides CRUD operations for animals using EF Core DbContext.
/// </summary>
public class EfAnimalsRepository : IAnimalsRepository
{
    private readonly AnimalZooContext _context;

    /// <summary>
    /// Initializes a new instance of the EfAnimalsRepository.
    /// </summary>
    /// <param name="context">The EF Core DbContext.</param>
    public EfAnimalsRepository(AnimalZooContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <summary>
    /// Adds a new animal or updates an existing one.
    /// Uses the UniqueId to determine if the animal exists.
    /// </summary>
    public void Save(Animal animal)
    {
        if (animal == null)
            throw new ArgumentNullException(nameof(animal));

        // Check if the animal already exists
        var existingAnimal = _context.Animals.Find(animal.UniqueId);

        if (existingAnimal == null)
        {
            // Add new animal
            _context.Animals.Add(animal);
        }
        else
        {
            // Update existing animal
            // Detach the existing tracked entity to avoid conflicts
            _context.Entry(existingAnimal).State = EntityState.Detached;

            // Attach and mark as modified
            _context.Animals.Attach(animal);
            _context.Entry(animal).State = EntityState.Modified;
        }

        _context.SaveChanges();
    }

    /// <summary>
    /// Deletes an animal by its unique identifier.
    /// </summary>
    /// <returns>True if the animal was found and deleted, false otherwise.</returns>
    public bool Delete(string uniqueId)
    {
        if (string.IsNullOrWhiteSpace(uniqueId))
            return false;

        var animal = _context.Animals.Find(uniqueId);
        if (animal == null)
            return false;

        _context.Animals.Remove(animal);
        _context.SaveChanges();
        return true;
    }

    /// <summary>
    /// Retrieves an animal by its unique identifier.
    /// </summary>
    /// <returns>The animal if found, null otherwise.</returns>
    public Animal? GetById(string uniqueId)
    {
        if (string.IsNullOrWhiteSpace(uniqueId))
            return null;

        return _context.Animals.Find(uniqueId);
    }

    /// <summary>
    /// Retrieves all animals from the database.
    /// </summary>
    /// <returns>A collection of all animals.</returns>
    public IEnumerable<Animal> GetAll()
    {
        return _context.Animals.ToList();
    }

    /// <summary>
    /// Finds animals matching the specified predicate.
    /// Note: This loads all animals into memory and then filters.
    /// For large datasets, consider using IQueryable-based methods.
    /// </summary>
    /// <returns>Filtered collection of animals.</returns>
    public IEnumerable<Animal> Find(Func<Animal, bool> predicate)
    {
        if (predicate == null)
            throw new ArgumentNullException(nameof(predicate));

        return _context.Animals.AsEnumerable().Where(predicate).ToList();
    }
}
