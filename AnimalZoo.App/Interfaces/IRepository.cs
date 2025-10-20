using System;
using System.Collections.Generic;

namespace AnimalZoo.App.Interfaces;

/// <summary>
/// Generic repository interface for basic CRUD-like operations.
/// </summary>
public interface IRepository<T>
{
    /// <summary>Add a single item.</summary>
    void Add(T item);

    /// <summary>Remove a single item.</summary>
    bool Remove(T item);

    /// <summary>Return all items.</summary>
    IEnumerable<T> GetAll();

    /// <summary>Find the first item that matches the predicate (compatible with LINQ).</summary>
    T? Find(Func<T, bool> predicate);
}
