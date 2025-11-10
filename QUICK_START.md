# Quick Start Guide

This guide shows you how to set up and run the AnimalZoo application with database persistence.

## Prerequisites

### macOS/Linux

1. **Install Docker Desktop**

   macOS:
   ```bash
   brew install --cask docker
   ```

   Linux: Download from [docker.com](https://docs.docker.com/desktop/install/linux-install/)

   Then launch Docker Desktop and wait for it to start.

2. **Verify Docker is Running**

   ```bash
   docker --version
   ```

### Windows

1. **Install Docker Desktop**

   Download and install from [docker.com/products/docker-desktop](https://www.docker.com/products/docker-desktop)

   After installation, launch Docker Desktop and wait for it to start.

2. **Verify Docker is Running**

   Open PowerShell or Command Prompt:
   ```powershell
   docker --version
   ```

3. **Note on Make Commands**

   Windows doesn't have `make` by default. You can either:
   - **Option A**: Use WSL (Windows Subsystem for Linux) and follow macOS/Linux instructions
   - **Option B**: Use PowerShell/CMD and follow "Method B: Manual Commands" below
   - **Option C**: Install `make` via Chocolatey: `choco install make`

---

## Setup Methods

Choose one of two methods:

- **Method A**: Using Make commands (recommended for macOS/Linux or Windows with WSL)
- **Method B**: Using manual Docker and dotnet commands (recommended for Windows)

---

## Method A: Using Make Commands

> **For**: macOS, Linux, or Windows with WSL/make installed

### Quick 3-Step Setup

```bash
# 1. Start SQL Server
make docker-up

# 2. Initialize database schema
make docker-init

# 3. Run the application
make run
```

That's it! The application will start with an empty database.

### View All Available Commands

```bash
make help
```

See `MAKE.md` for detailed documentation of all make commands.

---

## Method B: Using Manual Commands

> **For**: All platforms (Windows, macOS, Linux)

### Step 1: Start SQL Server

**Using Docker Compose** (recommended):

> **Windows users**: Use PowerShell or Command Prompt for all commands below

```bash
# Copy environment variables template
# macOS/Linux:
cp .env.example .env
# Windows (PowerShell):
# copy .env.example .env

# Start SQL Server with Docker Compose
docker compose up -d

# Wait for SQL Server to be ready (check logs)
docker logs animalzoo-sqlserver
```

**Using Docker CLI directly**:

```bash
docker run -e "ACCEPT_EULA=Y" \
  -e "MSSQL_SA_PASSWORD=YourStrong@Passw0rd" \
  -p 1433:1433 \
  --name animalzoo-sqlserver \
  --platform linux/amd64 \
  -v sqlserver_data:/var/opt/mssql \
  -d mcr.microsoft.com/mssql/server:2022-latest
```

**Check if SQL Server is ready**:

```bash
docker logs animalzoo-sqlserver | grep "ready for client connections"
```

---

### Step 2: Initialize Database Schema

```bash
# Copy SQL script to container
docker cp database-init.sql animalzoo-sqlserver:/database-init.sql

# Execute initialization script
docker exec animalzoo-sqlserver /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P "YourStrong@Passw0rd" -C \
  -i /database-init.sql
```

You should see: "Database initialization completed successfully."

---

### Step 3: Build and Run the Application

```bash
# Restore packages
dotnet restore AnimalZoo.sln

# Build the solution
dotnet build AnimalZoo.sln -c Debug

# Run the application
dotnet run --project AnimalZoo.App/AnimalZoo.App.csproj
```

---

## Verify It Works

1. **Application starts**: The window opens with an empty animal list
2. **Add an animal**: Click "Add Animal", fill in details, and save
3. **Check database**: The animal appears in the list
4. **Restart application**: Close and run again
5. **Verify persistence**: The animal you added is still there
6. **Check logs**: View `AnimalZoo.App/bin/Debug/net9.0/logs/animalzoo.log`

---

## Daily Workflow

### With Make Commands

```bash
# Check if database is running
make docker-status

# If not running, start it
make docker-up

# Run the application
make run
```

### Without Make Commands

```bash
# Check if container is running
docker ps | grep animalzoo-sqlserver

# If not running, start it
docker start animalzoo-sqlserver

# Run the application
dotnet run --project AnimalZoo.App/AnimalZoo.App.csproj
```

---

## Common Operations

### Clean Database (Keep Schema)

**With Make**:
```bash
make docker-clean-db
```

**Without Make**:
```bash
docker exec animalzoo-sqlserver /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P "YourStrong@Passw0rd" -C \
  -Q "USE AnimalZooDB; DELETE FROM Enclosures; DELETE FROM Animals;"
```

---

### View Database Logs

**With Make**:
```bash
# Show last 50 lines
make docker-logs-tail

# Follow logs in real-time
make docker-logs
```

**Without Make**:
```bash
# Show last 50 lines
docker logs --tail 50 animalzoo-sqlserver

# Follow logs in real-time
docker logs -f animalzoo-sqlserver
```

---

### Test Database Connection

**With Make**:
```bash
make docker-test-connection
```

**Without Make**:
```bash
docker exec animalzoo-sqlserver /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P "YourStrong@Passw0rd" -C \
  -Q "SELECT @@VERSION;"
```

---

### Open SQL Command Line

**With Make**:
```bash
make docker-exec
```

**Without Make**:
```bash
docker exec -it animalzoo-sqlserver /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P "YourStrong@Passw0rd" -C
```

Example queries:
```sql
SELECT @@VERSION;
GO

USE AnimalZooDB;
GO

SELECT * FROM Animals;
GO

SELECT * FROM Enclosures;
GO

exit
```

---

### Stop and Start

**With Make**:
```bash
# Stop database
make docker-down

# Start database
make docker-up

# Restart database
make docker-restart
```

**Without Make** (Docker Compose):
```bash
# Stop database
docker compose down

# Start database
docker compose up -d

# Restart database
docker compose restart
```

**Without Make** (Docker CLI):
```bash
# Stop database
docker stop animalzoo-sqlserver

# Start database
docker start animalzoo-sqlserver

# Restart database
docker restart animalzoo-sqlserver
```

---

### Complete Reset (Delete All Data)

**With Make**:
```bash
make docker-rebuild
```

**Without Make** (Docker Compose):
```bash
# Remove container and volumes
docker compose down -v

# Start fresh
docker compose up -d

# Initialize database
docker cp database-init.sql animalzoo-sqlserver:/database-init.sql
docker exec animalzoo-sqlserver /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P "YourStrong@Passw0rd" -C \
  -i /database-init.sql
```

**Without Make** (Docker CLI):
```bash
# Remove container and volume
docker rm -f animalzoo-sqlserver
docker volume rm sqlserver_data

# Start from Step 1 again
```

---

## Configuration

### Environment Variables

Edit `.env` file to customize:

```bash
# Container name
DOCKER_NAME=animalzoo-sqlserver

# SQL Server password
SA_PASSWORD=YourStrong@Passw0rd

# Port mapping (change if 1433 is in use)
DB_PORT=1433
```

**Important**: After changing `SA_PASSWORD` or `DB_PORT`, update `AnimalZoo.App/appsettings.json` accordingly.

---

### Application Configuration

Edit `AnimalZoo.App/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "AnimalZooDb": "Server=localhost,1433;Database=AnimalZooDB;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True;Encrypt=True;"
  },
  "DataAccess": {
    "RepositoryType": "AdoNet"
  },
  "Logging": {
    "LoggerType": "Both",
    "JsonLogFilePath": "Logs/animalzoo.json",
    "XmlLogFilePath": "Logs/animalzoo.xml"
  }
}
```

**Data Access Options**:
- `"AdoNet"` - ADO.NET with direct SQL queries (default)
- `"EfCore"` - Entity Framework Core with LINQ and migrations

**Logger Options**:
- `"Json"` - JSON format only
- `"Xml"` - XML format only
- `"Both"` - Write to both JSON and XML simultaneously

---

## Using Entity Framework Core (Optional)

### Switch to EF Core

To use Entity Framework Core instead of ADO.NET:

1. **Update configuration** in `appsettings.json`:
   ```json
   {
     "DataAccess": {
       "RepositoryType": "EfCore"
     }
   }
   ```

2. **Apply migrations** (first time only):
   ```bash
   # Install EF Core tools (if not already installed)
   dotnet tool install --global dotnet-ef

   # Apply migrations to create/update database schema
   cd AnimalZoo.App
   dotnet ef database update
   ```

3. **Run the application**:
   ```bash
   dotnet run --project AnimalZoo.App/AnimalZoo.App.csproj
   ```

### EF Core Migration Commands

```bash
# View migration history
dotnet ef migrations list

# Create new migration
dotnet ef migrations add MigrationName --output-dir Data/Migrations

# Apply migrations to database
dotnet ef database update

# Revert to specific migration
dotnet ef database update PreviousMigrationName

# Remove last migration (if not applied)
dotnet ef migrations remove

# Generate SQL script from migrations
dotnet ef migrations script
```

### Switching Between ADO.NET and EF Core

Both implementations:
- Use the **same database schema**
- Support **all CRUD operations**
- Can be **switched at runtime** via configuration

No data migration needed - just change `RepositoryType` in `appsettings.json`.

## Troubleshooting

### Port 1433 Already in Use

**Solution**: Change the port

Edit `.env`:
```bash
DB_PORT=1434
```

Edit `appsettings.json`:
```json
"Server=localhost,1434;Database=..."
```

Then restart:
```bash
make docker-down && make docker-up    # With Make
docker compose down && docker compose up -d    # Without Make
```

---

### Container Won't Start

**Check logs**:
```bash
make docker-logs-tail    # With Make
docker logs animalzoo-sqlserver    # Without Make
```

**Common issues**:
- Port already in use (see above)
- Insufficient memory (Docker needs at least 2GB)
- Password doesn't meet requirements (8+ chars, mixed case, numbers, symbols)

---

### Database Connection Fails

1. **Verify container is running**:
   ```bash
   make docker-status    # With Make
   docker ps | grep animalzoo    # Without Make
   ```

2. **Test connection**:
   ```bash
   make docker-test-connection    # With Make
   ```

3. **Check logs**:
   ```bash
   make docker-logs-tail    # With Make
   ```

4. **Restart container**:
   ```bash
   make docker-restart    # With Make
   docker restart animalzoo-sqlserver    # Without Make
   ```

---

### Application Uses In-Memory Storage

The application falls back to in-memory storage if the database is unavailable.

**Check**:
- Container is running: `docker ps`
- Connection string in `appsettings.json` is correct
- Password matches between `.env` and `appsettings.json`

---

### Build Fails

```bash
# Clean and rebuild with Make (macOS/Linux/WSL)
make clean && make build

# Or manually:
dotnet clean AnimalZoo.sln

# macOS/Linux:
rm -rf **/bin **/obj

# Windows (PowerShell):
# Get-ChildItem -Path . -Include bin,obj -Recurse | Remove-Item -Recurse -Force

# Then build:
dotnet build AnimalZoo.sln
```

---

## Tips

- **Logs Location**:
  - macOS/Linux: `AnimalZoo.App/bin/Debug/net9.0/logs/`
  - Windows: `AnimalZoo.App\bin\Debug\net9.0\logs\`
- **Database Volume**: Data persists in Docker volume `animalzoo_sqlserver_data`
- **Azure Data Studio**: Use for visual database management (available for all platforms)
- **Hot Reload**: Changes to code require rebuild (`make build` or `dotnet build`)

---

## Performance Notes

- **First startup**: Takes 10-15 seconds for SQL Server to initialize
- **Subsequent startups**: Takes 2-3 seconds
- **Application startup**: Instant if database is already running

---

## Next Steps

- **MAKE.md** - Complete reference of all make commands
- **DATABASE_SETUP.md** - Database initialization details
- **README.md** - Project features and architecture
- **IMPLEMENTATION_SUMMARY.md** - Technical architecture details

---

## Summary

### macOS/Linux (Quickest Path with Make)
```bash
make docker-up && make docker-init && make run
```

### Windows / All Platforms (Manual Path)
```bash
# Copy .env file (Windows: use 'copy .env.example .env')
cp .env.example .env

# Start database
docker compose up -d

# Initialize database
docker cp database-init.sql animalzoo-sqlserver:/database-init.sql
docker exec animalzoo-sqlserver /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P "YourStrong@Passw0rd" -C \
  -i /database-init.sql

# Run application
dotnet run --project AnimalZoo.App/AnimalZoo.App.csproj
```

Happy coding! 🐾
