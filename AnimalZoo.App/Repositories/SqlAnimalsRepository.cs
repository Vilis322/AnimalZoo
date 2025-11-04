using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using AnimalZoo.App.Interfaces;
using AnimalZoo.App.Models;
using AnimalZoo.App.Utils;
using Microsoft.Data.SqlClient;

namespace AnimalZoo.App.Repositories;

/// <summary>
/// ADO.NET-based repository for animal persistence.
/// Uses parameterized queries to prevent SQL injection.
/// </summary>
public sealed class SqlAnimalsRepository : IAnimalsRepository
{
    private readonly string _connectionString;

    /// <summary>
    /// Initializes a new instance of the SqlAnimalsRepository.
    /// </summary>
    /// <param name="connectionString">SQL Server connection string.</param>
    public SqlAnimalsRepository(string connectionString)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
    }

    /// <inheritdoc />
    public void Save(Animal animal)
    {
        if (animal == null) throw new ArgumentNullException(nameof(animal));

        using var connection = new SqlConnection(_connectionString);
        connection.Open();

        // Check if animal already exists
        var exists = GetById(animal.UniqueId) != null;

        if (exists)
        {
            // Update existing animal
            var updateCmd = @"
                UPDATE Animals
                SET Name = @Name, Age = @Age, Mood = @Mood, AnimalType = @AnimalType
                WHERE UniqueId = @UniqueId";

            using var command = new SqlCommand(updateCmd, connection);
            command.Parameters.AddWithValue("@UniqueId", animal.UniqueId);
            command.Parameters.AddWithValue("@Name", animal.Name);
            command.Parameters.AddWithValue("@Age", animal.Age);
            command.Parameters.AddWithValue("@Mood", animal.Mood.ToString());
            command.Parameters.AddWithValue("@AnimalType", animal.GetType().Name);
            command.ExecuteNonQuery();
        }
        else
        {
            // Insert new animal
            var insertCmd = @"
                INSERT INTO Animals (UniqueId, Name, Age, Mood, AnimalType)
                VALUES (@UniqueId, @Name, @Age, @Mood, @AnimalType)";

            using var command = new SqlCommand(insertCmd, connection);
            command.Parameters.AddWithValue("@UniqueId", animal.UniqueId);
            command.Parameters.AddWithValue("@Name", animal.Name);
            command.Parameters.AddWithValue("@Age", animal.Age);
            command.Parameters.AddWithValue("@Mood", animal.Mood.ToString());
            command.Parameters.AddWithValue("@AnimalType", animal.GetType().Name);
            command.ExecuteNonQuery();
        }
    }

    /// <inheritdoc />
    public bool Delete(string uniqueId)
    {
        if (string.IsNullOrWhiteSpace(uniqueId)) throw new ArgumentNullException(nameof(uniqueId));

        using var connection = new SqlConnection(_connectionString);
        connection.Open();

        var deleteCmd = "DELETE FROM Animals WHERE UniqueId = @UniqueId";
        using var command = new SqlCommand(deleteCmd, connection);
        command.Parameters.AddWithValue("@UniqueId", uniqueId);

        var rowsAffected = command.ExecuteNonQuery();
        return rowsAffected > 0;
    }

    /// <inheritdoc />
    public Animal? GetById(string uniqueId)
    {
        if (string.IsNullOrWhiteSpace(uniqueId)) throw new ArgumentNullException(nameof(uniqueId));

        using var connection = new SqlConnection(_connectionString);
        connection.Open();

        var selectCmd = "SELECT UniqueId, Name, Age, Mood, AnimalType FROM Animals WHERE UniqueId = @UniqueId";
        using var command = new SqlCommand(selectCmd, connection);
        command.Parameters.AddWithValue("@UniqueId", uniqueId);

        using var reader = command.ExecuteReader();
        if (reader.Read())
        {
            return MapFromReader(reader);
        }

        return null;
    }

    /// <inheritdoc />
    public IEnumerable<Animal> GetAll()
    {
        var animals = new List<Animal>();

        using var connection = new SqlConnection(_connectionString);
        connection.Open();

        var selectCmd = "SELECT UniqueId, Name, Age, Mood, AnimalType FROM Animals";
        using var command = new SqlCommand(selectCmd, connection);
        using var reader = command.ExecuteReader();

        while (reader.Read())
        {
            var animal = MapFromReader(reader);
            if (animal != null)
            {
                animals.Add(animal);
            }
        }

        return animals;
    }

    /// <inheritdoc />
    public IEnumerable<Animal> Find(Func<Animal, bool> predicate)
    {
        return GetAll().Where(predicate);
    }

    /// <summary>
    /// Maps a database row to an Animal instance using reflection and AnimalFactory.
    /// </summary>
    private Animal? MapFromReader(SqlDataReader reader)
    {
        var uniqueId = reader.GetString(reader.GetOrdinal("UniqueId"));
        var name = reader.GetString(reader.GetOrdinal("Name"));
        var age = reader.GetDouble(reader.GetOrdinal("Age"));
        var moodStr = reader.GetString(reader.GetOrdinal("Mood"));
        var animalTypeName = reader.GetString(reader.GetOrdinal("AnimalType"));

        // Parse mood
        if (!Enum.TryParse<AnimalMood>(moodStr, out var mood))
        {
            mood = AnimalMood.Hungry;
        }

        // Find the type by name using reflection
        var animalType = typeof(Animal).Assembly.GetTypes()
            .FirstOrDefault(t => t.IsClass && !t.IsAbstract &&
                                  t.IsSubclassOf(typeof(Animal)) &&
                                  t.Name == animalTypeName);

        // Create animal using AnimalFactory
        var animal = AnimalFactory.Create(animalType, name, age);
        if (animal != null)
        {
            // Restore the original UniqueId and mood using reflection
            var uniqueIdField = typeof(Animal).GetProperty("UniqueId");
            if (uniqueIdField != null)
            {
                // UniqueId is readonly, so we need to use reflection to set it
                var backingField = typeof(Animal).GetField("<UniqueId>k__BackingField",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                backingField?.SetValue(animal, uniqueId);
            }

            animal.SetMood(mood);
        }

        return animal;
    }
}
