using System;
using System.Collections.Generic;
using AnimalZoo.App.Interfaces;
using Microsoft.Data.SqlClient;

namespace AnimalZoo.App.Repositories;

/// <summary>
/// ADO.NET-based repository for managing enclosure assignments.
/// Uses parameterized queries to prevent SQL injection.
/// </summary>
public sealed class SqlEnclosureRepository : IEnclosureRepository
{
    private readonly string _connectionString;

    /// <summary>
    /// Initializes a new instance of the SqlEnclosureRepository.
    /// </summary>
    /// <param name="connectionString">SQL Server connection string.</param>
    public SqlEnclosureRepository(string connectionString)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
    }

    /// <inheritdoc />
    public void AssignToEnclosure(string animalId, string enclosureName)
    {
        if (string.IsNullOrWhiteSpace(animalId)) throw new ArgumentNullException(nameof(animalId));
        if (string.IsNullOrWhiteSpace(enclosureName)) throw new ArgumentNullException(nameof(enclosureName));

        using var connection = new SqlConnection(_connectionString);
        connection.Open();

        // Check if assignment already exists
        var existingEnclosure = GetEnclosureName(animalId);

        if (existingEnclosure != null)
        {
            // Update existing assignment
            var updateCmd = "UPDATE Enclosures SET EnclosureName = @EnclosureName WHERE AnimalId = @AnimalId";
            using var command = new SqlCommand(updateCmd, connection);
            command.Parameters.AddWithValue("@AnimalId", animalId);
            command.Parameters.AddWithValue("@EnclosureName", enclosureName);
            command.ExecuteNonQuery();
        }
        else
        {
            // Insert new assignment
            var insertCmd = "INSERT INTO Enclosures (AnimalId, EnclosureName) VALUES (@AnimalId, @EnclosureName)";
            using var command = new SqlCommand(insertCmd, connection);
            command.Parameters.AddWithValue("@AnimalId", animalId);
            command.Parameters.AddWithValue("@EnclosureName", enclosureName);
            command.ExecuteNonQuery();
        }
    }

    /// <inheritdoc />
    public bool RemoveFromEnclosure(string animalId)
    {
        if (string.IsNullOrWhiteSpace(animalId)) throw new ArgumentNullException(nameof(animalId));

        using var connection = new SqlConnection(_connectionString);
        connection.Open();

        var deleteCmd = "DELETE FROM Enclosures WHERE AnimalId = @AnimalId";
        using var command = new SqlCommand(deleteCmd, connection);
        command.Parameters.AddWithValue("@AnimalId", animalId);

        var rowsAffected = command.ExecuteNonQuery();
        return rowsAffected > 0;
    }

    /// <inheritdoc />
    public string? GetEnclosureName(string animalId)
    {
        if (string.IsNullOrWhiteSpace(animalId)) throw new ArgumentNullException(nameof(animalId));

        using var connection = new SqlConnection(_connectionString);
        connection.Open();

        var selectCmd = "SELECT EnclosureName FROM Enclosures WHERE AnimalId = @AnimalId";
        using var command = new SqlCommand(selectCmd, connection);
        command.Parameters.AddWithValue("@AnimalId", animalId);

        var result = command.ExecuteScalar();
        return result?.ToString();
    }

    /// <inheritdoc />
    public IEnumerable<string> GetAnimalsByEnclosure(string enclosureName)
    {
        if (string.IsNullOrWhiteSpace(enclosureName)) throw new ArgumentNullException(nameof(enclosureName));

        var animalIds = new List<string>();

        using var connection = new SqlConnection(_connectionString);
        connection.Open();

        var selectCmd = "SELECT AnimalId FROM Enclosures WHERE EnclosureName = @EnclosureName";
        using var command = new SqlCommand(selectCmd, connection);
        command.Parameters.AddWithValue("@EnclosureName", enclosureName);

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            animalIds.Add(reader.GetString(0));
        }

        return animalIds;
    }

    /// <inheritdoc />
    public IEnumerable<string> GetAllEnclosureNames()
    {
        var enclosureNames = new List<string>();

        using var connection = new SqlConnection(_connectionString);
        connection.Open();

        var selectCmd = "SELECT DISTINCT EnclosureName FROM Enclosures ORDER BY EnclosureName";
        using var command = new SqlCommand(selectCmd, connection);

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            enclosureNames.Add(reader.GetString(0));
        }

        return enclosureNames;
    }
}
