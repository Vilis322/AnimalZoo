# Database Initialization Guide

This document explains the database schema and initialization process for the AnimalZoo application.

## Overview

The AnimalZoo application uses **SQL Server 2022** with a simple but effective schema design for storing animals and their enclosure assignments.

---

## Database Schema

### AnimalZooDB Database

The application uses a single database called `AnimalZooDB` with two main tables:

#### Animals Table

Stores all animal data with type discrimination.

| Column | Type | Description |
|--------|------|-------------|
| `UniqueId` | NVARCHAR(50) PRIMARY KEY | Unique identifier (8-char GUID prefix) |
| `Name` | NVARCHAR(100) NOT NULL | Animal's name |
| `Age` | FLOAT NOT NULL | Age in years (supports decimals) |
| `Mood` | NVARCHAR(50) NOT NULL | Current mood state (Hungry/Happy/Gaming/Sleeping) |
| `AnimalType` | NVARCHAR(50) NOT NULL | Type discriminator (Dog, Cat, Bird, Eagle, etc.) |
| `CreatedAt` | DATETIME2 DEFAULT GETUTCDATE() | Record creation timestamp |
| `UpdatedAt` | DATETIME2 DEFAULT GETUTCDATE() | Record update timestamp |

**Indexes:**
- `IX_Animals_AnimalType` - Improves queries filtering by animal type

#### Enclosures Table

Tracks which animals are assigned to which enclosures.

| Column | Type | Description |
|--------|------|-------------|
| `AnimalId` | NVARCHAR(50) PRIMARY KEY, FK | References Animals.UniqueId |
| `EnclosureName` | NVARCHAR(100) NOT NULL | Name of the enclosure |
| `AssignedAt` | DATETIME2 DEFAULT GETUTCDATE() | Assignment timestamp |

**Indexes:**
- `IX_Enclosures_EnclosureName` - Improves queries filtering by enclosure name

**Foreign Keys:**
- `FK_Enclosures_Animals` - Foreign key with CASCADE DELETE

---

## Initialization Process

### Method 1: Using Make (Recommended)

```bash
# Initialize database schema
make docker-init
```

### Method 2: Manual Execution

```bash
# Copy script to container
docker cp database-init.sql animalzoo-sqlserver:/database-init.sql

# Execute initialization script
docker exec animalzoo-sqlserver /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P "YourStrong@Passw0rd" -C \
  -i /database-init.sql
```

### Method 3: Using Azure Data Studio

1. Connect to `localhost,1433` with SA credentials
2. Open `database-init.sql`
3. Click "Run" or press F5

---

## Initialization Script Details

The `database-init.sql` script performs the following operations:

### 1. Database Creation

```sql
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'AnimalZooDB')
BEGIN
    CREATE DATABASE AnimalZooDB;
END
GO
```

Creates the database only if it doesn't already exist (idempotent).

### 2. Animals Table Creation

```sql
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Animals')
BEGIN
    CREATE TABLE Animals (
        UniqueId NVARCHAR(50) PRIMARY KEY,
        Name NVARCHAR(100) NOT NULL,
        Age FLOAT NOT NULL,
        Mood NVARCHAR(50) NOT NULL,
        AnimalType NVARCHAR(50) NOT NULL,
        CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2 DEFAULT GETUTCDATE()
    );
END
GO
```

### 3. Enclosures Table Creation

```sql
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Enclosures')
BEGIN
    CREATE TABLE Enclosures (
        AnimalId NVARCHAR(50) PRIMARY KEY,
        EnclosureName NVARCHAR(100) NOT NULL,
        AssignedAt DATETIME2 DEFAULT GETUTCDATE(),
        CONSTRAINT FK_Enclosures_Animals FOREIGN KEY (AnimalId)
            REFERENCES Animals(UniqueId) ON DELETE CASCADE
    );
END
GO
```

### 4. Index Creation

```sql
-- Index on EnclosureName for faster lookups
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Enclosures_EnclosureName')
BEGIN
    CREATE INDEX IX_Enclosures_EnclosureName ON Enclosures(EnclosureName);
END
GO

-- Index on AnimalType for faster type-based queries
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Animals_AnimalType')
BEGIN
    CREATE INDEX IX_Animals_AnimalType ON Animals(AnimalType);
END
GO
```

---

## Design Decisions

### Type Discrimination

Instead of creating separate tables for each animal type, we use a single `Animals` table with an `AnimalType` column. This approach:

- ✅ Simplifies queries and JOINs
- ✅ Reduces database complexity
- ✅ Makes adding new animal types trivial
- ✅ Aligns with the application's polymorphic model

The application uses **reflection and the AnimalFactory** to deserialize the correct concrete type at runtime.

### UniqueId Format

Animal IDs are 8-character prefixes of GUIDs (e.g., `a1b2c3d4`):

- Short enough to display in the UI
- Unique enough to avoid collisions
- Human-readable compared to full GUIDs
- Stable across database loads

### Float for Age

The `Age` column uses `FLOAT` instead of `INT` to support fractional ages (e.g., `2.5` years). This allows more precise age representation.

### Cascade Delete

The foreign key uses `ON DELETE CASCADE` so that deleting an animal automatically removes its enclosure assignment, maintaining referential integrity.

### Idempotent Script

All `CREATE` statements are wrapped in `IF NOT EXISTS` checks, making the script safe to run multiple times without errors.

---

## Verifying Initialization

### Check Database Exists

```sql
SELECT name FROM sys.databases WHERE name = 'AnimalZooDB';
GO
```

### Check Tables Exist

```sql
USE AnimalZooDB;
GO

SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES;
GO
```

Expected output:
- Animals
- Enclosures

### Check Indexes

```sql
SELECT
    OBJECT_NAME(object_id) AS TableName,
    name AS IndexName
FROM sys.indexes
WHERE OBJECT_NAME(object_id) IN ('Animals', 'Enclosures')
  AND name IS NOT NULL;
GO
```

Expected indexes:
- `IX_Animals_AnimalType`
- `IX_Enclosures_EnclosureName`

### Check Foreign Keys

```sql
SELECT
    OBJECT_NAME(parent_object_id) AS TableName,
    name AS ForeignKeyName,
    OBJECT_NAME(referenced_object_id) AS ReferencedTable
FROM sys.foreign_keys
WHERE OBJECT_NAME(parent_object_id) = 'Enclosures';
GO
```

Expected:
- `FK_Enclosures_Animals` referencing `Animals`

---

## Cleaning Database Data

### Keep Schema, Delete Data

**Using Make:**
```bash
make docker-clean-db
```

**Manually:**
```sql
USE AnimalZooDB;
GO

DELETE FROM Enclosures;  -- Delete first due to FK
DELETE FROM Animals;
GO
```

### Complete Reset

**Using Make:**
```bash
make docker-rebuild
```

**Manually:**
```bash
# Remove container and volumes
docker compose down -v

# Start fresh
docker compose up -d

# Initialize
docker cp database-init.sql animalzoo-sqlserver:/database-init.sql
docker exec animalzoo-sqlserver /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P "YourStrong@Passw0rd" -C \
  -i /database-init.sql
```

---

## Troubleshooting

### "Database already exists" Error

This is not actually an error. The script checks for existence before creating, so seeing this message means the database is already set up. Continue to the next step.

### "Object already exists" Error

Same as above - the script is idempotent and skips existing objects.

### Permission Denied Errors

Ensure you're using the SA account with the correct password. Check:
- `.env` file has correct `SA_PASSWORD`
- `appsettings.json` has matching password
- Container environment variable matches

### Foreign Key Constraint Errors

When deleting data, always delete from `Enclosures` before `Animals` due to the foreign key relationship.

Or use CASCADE DELETE (already configured):
```sql
DELETE FROM Animals;  -- Automatically deletes related Enclosures
```

---

## Schema Evolution

### Adding New Columns

To add new columns to existing tables without losing data:

1. Create a migration script (e.g., `migration-001.sql`)
2. Add the column with `ALTER TABLE`
3. Run the migration manually or via make command

Example:
```sql
USE AnimalZooDB;
GO

IF NOT EXISTS (
    SELECT * FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'Animals' AND COLUMN_NAME = 'BirthDate'
)
BEGIN
    ALTER TABLE Animals ADD BirthDate DATE NULL;
END
GO
```

### Version Control

Consider adding a `SchemaVersion` table to track migrations:

```sql
CREATE TABLE SchemaVersion (
    Version INT PRIMARY KEY,
    AppliedAt DATETIME2 DEFAULT GETUTCDATE(),
    Description NVARCHAR(200)
);
```

---

## Performance Considerations

### Current Indexes

The schema includes two indexes optimized for common queries:

1. **IX_Animals_AnimalType** - For type-based filtering and statistics
2. **IX_Enclosures_EnclosureName** - For enclosure-based lookups

### Query Patterns

The application performs these common operations:

- **GetAll()**: Full table scan (acceptable for small datasets)
- **GetById()**: Direct PK lookup (very fast)
- **Find by Type**: Uses IX_Animals_AnimalType
- **Enclosure lookups**: Uses IX_Enclosures_EnclosureName

### Scaling Recommendations

For larger datasets (>10,000 animals):

1. Add an index on `Animals.Name` for name-based searches
2. Consider partitioning by `AnimalType`
3. Add a covering index for the statistics query
4. Implement pagination in GetAll()

---

## Security Notes

### Connection String

The connection string uses:
- `TrustServerCertificate=True` - Required for local development
- `Encrypt=True` - Enables encryption (recommended)

**Production**: Replace self-signed certificate with a valid certificate authority (CA) certificate.

### SA Account

The setup uses the SA (System Administrator) account for simplicity.

**Production**: Create a dedicated application user with limited permissions:

```sql
CREATE LOGIN animalzoo_app WITH PASSWORD = 'StrongPassword123!';
CREATE USER animalzoo_app FOR LOGIN animalzoo_app;
GRANT SELECT, INSERT, UPDATE, DELETE ON Animals TO animalzoo_app;
GRANT SELECT, INSERT, UPDATE, DELETE ON Enclosures TO animalzoo_app;
```

---

## See Also

- **QUICK_START.md** - Setup and launch commands
- **MAKE.md** - Complete make commands reference
- **IMPLEMENTATION_SUMMARY.md** - Architecture details
- **README.md** - Project overview and features
