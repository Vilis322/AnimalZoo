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

### First-Time Setup
```bash
make docker-up      # Start SQL Server
make docker-init    # Create database schema
make run            # Run the application
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

### Complete Reset
```bash
make docker-rebuild     # Remove everything and start fresh
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

- **QUICK_START.md** - Quick setup guide with and without Make
- **DATABASE_SETUP.md** - Detailed database initialization information
- **README.md** - Project overview and features
- **IMPLEMENTATION_SUMMARY.md** - Architecture and design decisions
