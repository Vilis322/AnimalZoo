using System;
using System.Collections.Generic;
using System.Linq;
using AnimalZoo.App.Data;
using AnimalZoo.App.Interfaces;

namespace AnimalZoo.App.Repositories;

/// <summary>
/// Entity Framework Core implementation of the IEnclosureRepository interface.
/// Manages animal-to-enclosure assignments using EF Core DbContext.
/// </summary>
public class EfEnclosureRepository : IEnclosureRepository
{
    private readonly AnimalZooContext _context;

    /// <summary>
    /// Initializes a new instance of the EfEnclosureRepository.
    /// </summary>
    /// <param name="context">The EF Core DbContext.</param>
    public EfEnclosureRepository(AnimalZooContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <summary>
    /// Assigns an animal to an enclosure.
    /// Creates a new assignment or updates an existing one.
    /// </summary>
    public void AssignToEnclosure(string animalId, string enclosureName)
    {
        if (string.IsNullOrWhiteSpace(animalId))
            throw new ArgumentException("Animal ID cannot be null or empty.", nameof(animalId));

        if (string.IsNullOrWhiteSpace(enclosureName))
            throw new ArgumentException("Enclosure name cannot be null or empty.", nameof(enclosureName));

        // Check if assignment already exists
        var existingAssignment = _context.Enclosures.Find(animalId);

        if (existingAssignment == null)
        {
            // Create new assignment
            var assignment = new AnimalEnclosureAssignment
            {
                AnimalId = animalId,
                EnclosureName = enclosureName,
                AssignedAt = DateTime.UtcNow
            };
            _context.Enclosures.Add(assignment);
        }
        else
        {
            // Update existing assignment
            existingAssignment.EnclosureName = enclosureName;
            existingAssignment.AssignedAt = DateTime.UtcNow;
        }

        _context.SaveChanges();
    }

    /// <summary>
    /// Removes an animal from its enclosure.
    /// </summary>
    /// <returns>True if the assignment was found and removed, false otherwise.</returns>
    public bool RemoveFromEnclosure(string animalId)
    {
        if (string.IsNullOrWhiteSpace(animalId))
            return false;

        var assignment = _context.Enclosures.Find(animalId);
        if (assignment == null)
            return false;

        _context.Enclosures.Remove(assignment);
        _context.SaveChanges();
        return true;
    }

    /// <summary>
    /// Gets the enclosure name for a specific animal.
    /// </summary>
    /// <returns>The enclosure name if assigned, null otherwise.</returns>
    public string? GetEnclosureName(string animalId)
    {
        if (string.IsNullOrWhiteSpace(animalId))
            return null;

        var assignment = _context.Enclosures.Find(animalId);
        return assignment?.EnclosureName;
    }

    /// <summary>
    /// Gets all animal IDs assigned to a specific enclosure.
    /// </summary>
    /// <returns>A collection of animal unique identifiers.</returns>
    public IEnumerable<string> GetAnimalsByEnclosure(string enclosureName)
    {
        if (string.IsNullOrWhiteSpace(enclosureName))
            return Enumerable.Empty<string>();

        return _context.Enclosures
            .Where(e => e.EnclosureName == enclosureName)
            .Select(e => e.AnimalId)
            .ToList();
    }

    /// <summary>
    /// Gets all unique enclosure names.
    /// </summary>
    /// <returns>A collection of unique enclosure names.</returns>
    public IEnumerable<string> GetAllEnclosureNames()
    {
        return _context.Enclosures
            .Select(e => e.EnclosureName)
            .Distinct()
            .OrderBy(name => name)
            .ToList();
    }
}
