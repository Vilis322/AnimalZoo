namespace AnimalZoo.App.Data;

/// <summary>
/// Represents the mapping between an animal and its enclosure.
/// This is a simple POCO for EF Core persistence.
/// </summary>
public class AnimalEnclosureAssignment
{
    /// <summary>
    /// The unique identifier of the animal.
    /// This serves as the primary key and foreign key to the Animals table.
    /// </summary>
    public string AnimalId { get; set; } = string.Empty;

    /// <summary>
    /// The name of the enclosure where the animal resides.
    /// </summary>
    public string EnclosureName { get; set; } = string.Empty;

    /// <summary>
    /// The timestamp when the animal was assigned to this enclosure.
    /// </summary>
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
}
