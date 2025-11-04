using System;
using System.Collections.Generic;
using System.Linq;
using AnimalZoo.App.Interfaces;
using AnimalZoo.App.Models;

namespace AnimalZoo.App.Repositories;

/// <summary>
/// Adapter that wraps InMemoryRepository to implement IAnimalsRepository.
/// Used as a fallback when no SQL repository is configured.
/// </summary>
internal sealed class InMemoryRepositoryAdapter : IAnimalsRepository
{
    private readonly InMemoryRepository<Animal> _inner = new();

    public void Save(Animal animal)
    {
        // InMemoryRepository doesn't have update logic, so we remove and re-add
        var existing = _inner.Find(a => a.UniqueId == animal.UniqueId);
        if (existing != null)
        {
            _inner.Remove(existing);
        }
        _inner.Add(animal);
    }

    public bool Delete(string uniqueId)
    {
        var animal = _inner.Find(a => a.UniqueId == uniqueId);
        if (animal == null) return false;
        return _inner.Remove(animal);
    }

    public Animal? GetById(string uniqueId)
    {
        return _inner.Find(a => a.UniqueId == uniqueId);
    }

    public IEnumerable<Animal> GetAll()
    {
        return _inner.GetAll();
    }

    public IEnumerable<Animal> Find(Func<Animal, bool> predicate)
    {
        return _inner.GetAll().Where(predicate);
    }
}

/// <summary>
/// In-memory implementation of IEnclosureRepository.
/// Used as a fallback when no SQL repository is configured.
/// </summary>
internal sealed class InMemoryEnclosureRepositoryAdapter : IEnclosureRepository
{
    private readonly Dictionary<string, string> _assignments = new();

    public void AssignToEnclosure(string animalId, string enclosureName)
    {
        _assignments[animalId] = enclosureName;
    }

    public bool RemoveFromEnclosure(string animalId)
    {
        return _assignments.Remove(animalId);
    }

    public string? GetEnclosureName(string animalId)
    {
        return _assignments.TryGetValue(animalId, out var name) ? name : null;
    }

    public IEnumerable<string> GetAnimalsByEnclosure(string enclosureName)
    {
        return _assignments.Where(kvp => kvp.Value == enclosureName).Select(kvp => kvp.Key);
    }

    public IEnumerable<string> GetAllEnclosureNames()
    {
        return _assignments.Values.Distinct().OrderBy(n => n);
    }
}
