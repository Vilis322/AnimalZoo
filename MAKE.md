# Makefile Commands Reference

This document provides detailed information about all available `make` commands for the AnimalZoo project.

## Quick Reference

Run `make` or `make help` to see all available commands with descriptions.

---

## .NET Build Commands

### `make restore`
Restores NuGet packages for the solution.

```bash
make restore
```

**Output:** Downloads and installs all package dependencies defined in the project files.

---

### `make build`
Builds the entire solution in Debug configuration. Automatically runs `restore` first.

```bash
make build
```

**What it does:**
- Restores NuGet packages (if not already restored)
- Compiles all projects in the solution
- Outputs binaries to `bin/Debug/net9.0/`

**Output location:** `AnimalZoo.App/bin/Debug/net9.0/AnimalZoo.App.dll`

---

### `make run`
Builds and runs the AnimalZoo application. Automatically runs `build` first.

```bash
make run
```

**What it does:**
- Restores packages
- Builds the solution
- Launches the Avalonia desktop application

**Requirements:** SQL Server container must be running (`make docker-up`) for database features.

---

### `make clean`
Cleans all build artifacts (bin and obj directories).

```bash
make clean
```

**What it does:**
- Runs `dotnet clean`
- Removes all `bin/` and `obj/` directories recursively

**Use case:** Clean slate before rebuilding or when switching branches.

---

## Docker Management Commands

### `make docker-up`
Starts the SQL Server 2022 container using Docker Compose.

```bash
make docker-up
```

**What it does:**
- Creates `.env` from `.env.example` if it doesn't exist
- Starts SQL Server container in detached mode
- Creates persistent volumes for data storage
- Waits up to 30 seconds for SQL Server to be ready

**Output:** Container name, status, and readiness confirmation.

**Note:** If port 1433 is already in use, edit `.env` to change `DB_PORT`.

---

### `make docker-down`
Stops and removes the SQL Server container.

```bash
make docker-down
```

**What it does:**
- Stops the running container
- Removes the container
- Preserves data volumes (data persists)

**Note:** To also delete data volumes, use `make docker-clean` instead.

---

### `make docker-restart`
Restarts the SQL Server container without removing it.

```bash
make docker-restart
```

**What it does:**
- Restarts the existing container
- Waits 5 seconds for SQL Server to be ready
- Preserves all data and configuration

**Use case:** Apply configuration changes or recover from a hung state.

---

### `make docker-status`
Shows detailed container status and health information.

```bash
make docker-status
```

**Output:**
- Container running status (RUNNING/STOPPED/NOT FOUND)
- Uptime and health check status
- Port mappings
- Docker Compose service status

---

### `make docker-logs`
Shows SQL Server logs in follow mode (live streaming).

```bash
make docker-logs
```

**What it does:**
- Displays container logs in real-time
- Follows new log entries as they appear
- Press `Ctrl+C` to exit

**Use case:** Debugging connection issues or monitoring SQL Server activity.

---

### `make docker-logs-tail`
Shows the last 50 lines of SQL Server logs.

```bash
make docker-logs-tail
```

**Output:** Last 50 log entries from the container.

**Use case:** Quick check of recent activity without following live logs.

---

## Database Operations

### `make docker-init`
Initializes the database schema by running the SQL initialization script.

```bash
make docker-init
```

**What it does:**
- Copies `database-init.sql` to the container
- Executes the script to create:
  - `AnimalZooDB` database
  - `Animals` table
  - `Enclosures` table
  - Indexes for performance

**Requirements:** Container must be running (`make docker-up`).

**Run this:** After first `docker-up` or after `docker-rebuild`.

---

### `make docker-exec`
Opens an interactive SQL command line (sqlcmd) inside the container.

```bash
make docker-exec
```

**What it does:**
- Launches `sqlcmd` with SA credentials
- Provides interactive SQL prompt

**Example usage:**
```sql
SELECT @@VERSION;
GO

USE AnimalZooDB;
GO

SELECT * FROM Animals;
GO

exit
```

**Tip:** Every SQL statement must be followed by `GO` to execute.

---

### `make docker-test-connection`
Tests the database connection and displays SQL Server version.

```bash
make docker-test-connection
```

**What it does:**
- Connects to SQL Server
- Executes `SELECT @@VERSION`
- Displays connection status (✓ success or ✗ failed)

**Use case:** Verify SQL Server is accessible before running the application.

---

### `make docker-clean-db`
Deletes all data from Animals and Enclosures tables (keeps schema).

```bash
make docker-clean-db
```

**What it does:**
- Prompts for confirmation
- Deletes all rows from `Enclosures` table
- Deletes all rows from `Animals` table
- Preserves table structure and indexes

**Warning:** This action cannot be undone. All animal data will be lost.

**Use case:** Start fresh without rebuilding the entire database.

---

## Data Access Configuration

### `make show-data-access`
Displays the currently active data access implementation.

```bash
make show-data-access
```

**Output:** Shows whether the application is using ADO.NET or Entity Framework Core repositories.

**Example:**
```
Current Data Access Configuration:
  Type: AdoNet
```

---

### `make use-adonet`
Switches the application to use ADO.NET repositories (default).

```bash
make use-adonet
```

**What it does:**
- Modifies `appsettings.json` to set `RepositoryType: "AdoNet"`
- Displays confirmation message
- Shows note about database initialization

**Requirements:** Use `make docker-init` to initialize database with SQL script.

**Use case:** Switch back to ADO.NET after testing EF Core.

---

### `make use-efcore`
Switches the application to use Entity Framework Core repositories.

```bash
make use-efcore
```

**What it does:**
- Modifies `appsettings.json` to set `RepositoryType: "EfCore"`
- Displays confirmation message
- Shows reminder to run migrations

**Requirements:** Run `make ef-init` to apply EF Core migrations to the database.

**Use case:** Enable EF Core features like automatic change tracking and LINQ queries.

---

## Entity Framework Core Commands

### `make ef-init`
Applies EF Core migrations to create or update the database schema (first time setup for empty database).

```bash
make ef-init
```

**What it does:**
- Installs `dotnet-ef` tool if not already installed
- Checks if SQL Server container is running
- Applies all pending migrations to the database
- Creates/updates tables, indexes, and foreign keys
- Detects if database was created with ADO.NET and suggests solution

**Requirements:**
- SQL Server container must be running (`make docker-up`)
- Application must be configured for EF Core (`make use-efcore`)
- Database should be empty (not initialized with `docker-init`)

**Output:** Creates the same database schema as `docker-init`.

**Run this:** After switching to EF Core for the first time with an empty database.

**Note:** If you get an error about existing tables, use `make ef-init-existing` instead.

---

### `make ef-init-existing`
Marks an existing ADO.NET database as migrated (for switching from ADO.NET to EF Core).

```bash
make ef-init-existing
```

**What it does:**
- Creates `__EFMigrationsHistory` table if it doesn't exist
- Inserts a record marking InitialCreate migration as applied
- Tells EF Core that the database schema is already up to date
- Allows EF Core to work with databases created by `docker-init`

**Requirements:**
- SQL Server container must be running
- Database was created with `make docker-init` (ADO.NET script)
- Application is configured for EF Core (`make use-efcore`)

**Use case:**
- Switching from ADO.NET to EF Core with existing database
- Database already has Animals and Enclosures tables
- Want to use EF Core without recreating the database

**Example workflow:**
```bash
# You have an existing ADO.NET database
make use-efcore              # Switch to EF Core
make ef-init-existing        # Mark existing database as migrated
make run                     # Run with EF Core
```

---

### `make ef-update`
Applies pending EF Core migrations (alias for `ef-init`).

```bash
make ef-update
```

**What it does:**
- Same as `make ef-init`
- Applies any new migrations that haven't been applied yet

**Use case:** Update database schema after pulling changes with new migrations.

---

### `make ef-migrate`
Creates a new EF Core migration.

```bash
make ef-migrate NAME=MigrationName
```

**What it does:**
- Creates a new migration file in `AnimalZoo.App/Data/Migrations/`
- Generates code to apply and revert the migration
- Uses the specified NAME for the migration

**Requirements:** Migration name must be specified via `NAME=` parameter.

**Example:**
```bash
make ef-migrate NAME=AddAnimalBirthDate
```

**Output:** Creates three files:
- `YYYYMMDDHHMMSS_MigrationName.cs` - Migration code
- `YYYYMMDDHHMMSS_MigrationName.Designer.cs` - Migration metadata
- Updates `AnimalZooContextModelSnapshot.cs`

**Use case:** Create a migration after modifying entity models or DbContext configuration.

---

### `make ef-migrations-list`
Lists all EF Core migrations and their status.

```bash
make ef-migrations-list
```

**Output:**
- All migrations in the project
- Applied migrations (✓)
- Pending migrations (not yet applied)

**Example output:**
```
20251110134623_InitialCreate (Applied)
20251115120000_AddAnimalBirthDate (Pending)
```

**Use case:** Check which migrations have been applied to the database.

---

### `make ef-migrations-remove`
Removes the last migration (if not yet applied to database).

```bash
make ef-migrations-remove
```

**What it does:**
- Removes the most recent migration
- Deletes migration files from disk
- Reverts the model snapshot

**Requirements:** Migration must not be applied to the database yet.

**Warning:** Cannot remove migrations that have already been applied. Use `dotnet ef database update PreviousMigrationName` to revert first.

**Use case:** Fix a mistake in a migration before applying it.

---

## Maintenance Commands

### `make docker-clean`
Removes the container and all associated volumes (deletes all data).

```bash
make docker-clean
```

**What it does:**
- Prompts for confirmation (Y/N)
- Stops the container
- Removes the container
- Deletes all data volumes (complete data loss)

**Warning:** This permanently deletes all database data.

**Use case:**
- Resolve persistent container issues
- Start completely fresh
- Free up disk space

---

### `make docker-rebuild`
Complete clean slate: removes everything and starts fresh.

```bash
make docker-rebuild
```

**What it does:**
1. Runs `docker-clean` (with confirmation)
2. Runs `docker-up` (starts new container)
3. Runs `docker-init` (creates schema)

**Equivalent to:**
```bash
make docker-clean
make docker-up
make docker-init
```

**Use case:** Fix corrupted database or start with clean state.

---

## Environment Variables

All Docker commands respect these environment variables (defined in `.env`):

| Variable | Default | Description |
|----------|---------|-------------|
| `DOCKER_NAME` | `animalzoo-sqlserver` | Container name |
| `SA_PASSWORD` | `YourStrong@Passw0rd` | SA password (change in production!) |
| `DB_PORT` | `1433` | Host port mapping |

### Overriding Variables

You can override variables on the command line:

```bash
# Use a different port
make docker-up DB_PORT=1434

# Use a custom password
make docker-up SA_PASSWORD="MySecurePass123!"
```

---

## Common Workflows

### First-Time Setup (ADO.NET - Default)
```bash
make docker-up      # Start SQL Server
make docker-init    # Create database schema with SQL script
make run            # Run the application
```

### First-Time Setup (Entity Framework Core - Empty Database)
```bash
make docker-up      # Start SQL Server
make use-efcore     # Switch to EF Core
make ef-init        # Apply EF Core migrations
make run            # Run the application
```

### Migrating from ADO.NET to EF Core (Existing Database)
```bash
# You already have data from ADO.NET
make use-efcore           # Switch to EF Core
make ef-init-existing     # Mark database as migrated
make run                  # Run with EF Core (keeps all data)
```

### Switching Between ADO.NET and EF Core

**From ADO.NET to EF Core (with existing database):**
```bash
make use-efcore              # Switch configuration
make ef-init-existing        # Mark existing database as migrated
make run                     # Run with EF Core
```

**From ADO.NET to EF Core (with empty database):**
```bash
make use-efcore              # Switch configuration
make ef-init                 # Apply migrations
make run                     # Run with EF Core
```

**From EF Core back to ADO.NET:**
```bash
make use-adonet              # Switch configuration
make run                     # Run with ADO.NET (no database changes needed)
```

**Check current configuration:**
```bash
make show-data-access
```

### Daily Development
```bash
make docker-status  # Check if container is running
make run            # Build and run the app
```

### Clean Database (Keep Schema)
```bash
make docker-clean-db    # Delete all animals
make run                # Start with empty database
```

### Complete Reset (ADO.NET)
```bash
make docker-rebuild     # Remove everything and start fresh
make use-adonet         # Ensure ADO.NET is active
make run                # Run with clean database
```

### Complete Reset (EF Core)
```bash
make docker-rebuild     # Remove everything and start fresh
make use-efcore         # Switch to EF Core
make ef-init            # Apply migrations
make run                # Run with clean database
```

### Debugging Database Issues
```bash
make docker-logs-tail         # Check recent logs
make docker-test-connection   # Verify connectivity
make docker-exec              # Run manual SQL queries
```

---

## Troubleshooting

### Command Fails with "Container not running"
```bash
# Check status
make docker-status

# Start container
make docker-up
```

### Port 1433 Already in Use
```bash
# Edit .env file
DB_PORT=1434

# Restart with new port
make docker-down
make docker-up

# Update appsettings.json
Server=localhost,1434;...
```

### Database Connection Fails
```bash
# Verify container is healthy
make docker-status

# Check logs for errors
make docker-logs-tail

# Test connection
make docker-test-connection

# If needed, restart
make docker-restart
```

### Application Won't Build
```bash
# Clean and rebuild
make clean
make build
```

### Want to Start Completely Fresh
```bash
# Nuclear option: delete everything
make docker-rebuild
make clean
make run
```

---

## Advanced Usage

### Running Commands in Sequence
```bash
# Automatically run multiple commands
make docker-up && make docker-init && make run
```

### Running Without Make

If `make` is not available, see `QUICK_START.md` for equivalent Docker and `dotnet` commands.

---

## Color Coding in Terminal

The Makefile uses color-coded output:

- **🔵 Blue**: Informational messages
- **🟢 Green**: Success messages
- **🟡 Yellow**: Warnings
- **🔴 Red**: Errors or destructive actions

---

## See Also

- **[QUICK_START.md](./QUICK_START.md)** - Quick setup guide with and without Make
- **[DATABASE_SETUP.md](./DATABASE_SETUP.md)** - Detailed database initialization information
- **[README.md](./README.md)** - Project overview and features
- **[IMPLEMENTATION_SUMMARY.md](./IMPLEMENTATION_SUMMARY.md)** - Architecture and design decisions
