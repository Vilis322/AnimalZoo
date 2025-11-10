using AnimalZoo.App.Models;
using Microsoft.EntityFrameworkCore;

namespace AnimalZoo.App.Data;

/// <summary>
/// Entity Framework Core DbContext for the AnimalZoo application.
/// Provides access to Animals and Enclosure assignments with fluent configuration.
/// Uses Table-Per-Hierarchy (TPH) pattern for animal inheritance.
/// </summary>
public class AnimalZooContext : DbContext
{
    /// <summary>
    /// DbSet for all animals (base class and derived types).
    /// </summary>
    public DbSet<Animal> Animals { get; set; }

    /// <summary>
    /// DbSet for animal-to-enclosure assignments.
    /// </summary>
    public DbSet<AnimalEnclosureAssignment> Enclosures { get; set; }

    /// <summary>
    /// Constructor accepting DbContextOptions for configuration.
    /// </summary>
    public AnimalZooContext(DbContextOptions<AnimalZooContext> options) : base(options)
    {
    }

    /// <summary>
    /// Configures the entity mappings using Fluent API.
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureAnimals(modelBuilder);
        ConfigureEnclosures(modelBuilder);
    }

    /// <summary>
    /// Configures the Animals entity with Table-Per-Hierarchy (TPH) inheritance pattern.
    /// </summary>
    private void ConfigureAnimals(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Animal>(entity =>
        {
            // Table name
            entity.ToTable("Animals");

            // Primary key
            entity.HasKey(a => a.UniqueId);

            // Properties
            entity.Property(a => a.UniqueId)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(a => a.Name)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(a => a.Age)
                .IsRequired();

            entity.Property(a => a.Mood)
                .HasMaxLength(50)
                .HasConversion<string>() // Store enum as string
                .IsRequired();

            // Discriminator column for TPH (Table Per Hierarchy)
            entity.HasDiscriminator<string>("AnimalType")
                .HasValue<Cat>("Cat")
                .HasValue<Dog>("Dog")
                .HasValue<Bird>("Bird")
                .HasValue<Eagle>("Eagle")
                .HasValue<Bat>("Bat")
                .HasValue<Lion>("Lion")
                .HasValue<Penguin>("Penguin")
                .HasValue<Fox>("Fox")
                .HasValue<Parrot>("Parrot")
                .HasValue<Monkey>("Monkey")
                .HasValue<Raccoon>("Raccoon")
                .HasValue<Turtle>("Turtle");

            // Index on discriminator for query performance
            entity.HasIndex("AnimalType")
                .HasDatabaseName("IX_Animals_AnimalType");

            // Ignore properties that are not persisted
            entity.Ignore(a => a.DisplayState);
            entity.Ignore(a => a.Identifier);
        });

        // Configure derived types to ignore runtime properties
        modelBuilder.Entity<Bird>().Ignore(b => b.IsFlying);
        modelBuilder.Entity<Eagle>().Ignore(e => e.IsFlying);
        modelBuilder.Entity<Bat>().Ignore(b => b.IsFlying);
        modelBuilder.Entity<Parrot>().Ignore(p => p.IsFlying);
    }

    /// <summary>
    /// Configures the Enclosures entity (animal-to-enclosure assignments).
    /// </summary>
    private void ConfigureEnclosures(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AnimalEnclosureAssignment>(entity =>
        {
            // Table name
            entity.ToTable("Enclosures");

            // Primary key
            entity.HasKey(e => e.AnimalId);

            // Properties
            entity.Property(e => e.AnimalId)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(e => e.EnclosureName)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(e => e.AssignedAt)
                .IsRequired()
                .HasDefaultValueSql("GETUTCDATE()");

            // Index on EnclosureName for query performance
            entity.HasIndex(e => e.EnclosureName)
                .HasDatabaseName("IX_Enclosures_EnclosureName");

            // Foreign key relationship with cascade delete
            entity.HasOne<Animal>()
                .WithOne()
                .HasForeignKey<AnimalEnclosureAssignment>(e => e.AnimalId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
