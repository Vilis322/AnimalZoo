-- AnimalZoo Database Initialization Script
-- This script creates the database and tables for the AnimalZoo application

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'AnimalZooDB')
BEGIN
    CREATE DATABASE AnimalZooDB;
END
GO

USE AnimalZooDB;
GO

-- Create Animals table
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

    PRINT 'Animals table created successfully.';
END
ELSE
BEGIN
    PRINT 'Animals table already exists.';
END
GO

-- Create Enclosures table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Enclosures')
BEGIN
    CREATE TABLE Enclosures (
        AnimalId NVARCHAR(50) PRIMARY KEY,
        EnclosureName NVARCHAR(100) NOT NULL,
        AssignedAt DATETIME2 DEFAULT GETUTCDATE(),
        CONSTRAINT FK_Enclosures_Animals FOREIGN KEY (AnimalId)
            REFERENCES Animals(UniqueId) ON DELETE CASCADE
    );

    PRINT 'Enclosures table created successfully.';
END
ELSE
BEGIN
    PRINT 'Enclosures table already exists.';
END
GO

-- Create index for faster lookups
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Enclosures_EnclosureName')
BEGIN
    CREATE INDEX IX_Enclosures_EnclosureName ON Enclosures(EnclosureName);
    PRINT 'Index on EnclosureName created successfully.';
END
GO

-- Create index for animal type queries
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Animals_AnimalType')
BEGIN
    CREATE INDEX IX_Animals_AnimalType ON Animals(AnimalType);
    PRINT 'Index on AnimalType created successfully.';
END
GO

PRINT 'Database initialization completed successfully.';
GO
