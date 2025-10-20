using System;
using System.Collections.Generic;
using System.Linq;
using AnimalZoo.App.Interfaces;

namespace AnimalZoo.App.Repositories;

/// <summary>
/// Simple in-memory repository backed by a List<T>.
/// </summary>
public sealed class InMemoryRepository<T> : IRepository<T>
{
    private readonly List<T> _items = new();

    public void Add(T item) => _items.Add(item);

    public bool Remove(T item) => _items.Remove(item);

    public IEnumerable<T> GetAll() => _items; // returning the list is OK for read-only enumerations

    public T? Find(Func<T, bool> predicate) => _items.FirstOrDefault(predicate);
}
