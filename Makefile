# AnimalZoo Project Makefile
# Provides automation for both .NET build tasks and Docker-based SQL Server management

# .NET Project Configuration
SOLUTION = AnimalZoo.sln
APP = AnimalZoo.App/AnimalZoo.App.csproj

# Docker SQL Server Configuration
DOCKER_NAME ?= animalzoo-sqlserver
SA_PASSWORD ?= YourStrong@Passw0rd
DB_PORT ?= 1433
SQL_IMAGE ?= mcr.microsoft.com/mssql/server:2022-latest
DB_NAME = AnimalZooDB
PLATFORM = linux/amd64

# Docker Compose file
COMPOSE_FILE = docker-compose.yml

# SQL initialization script
INIT_SCRIPT = database-init.sql

# Configuration file
APPSETTINGS = AnimalZoo.App/appsettings.json

# Colors for terminal output
COLOR_RESET = \033[0m
COLOR_GREEN = \033[32m
COLOR_YELLOW = \033[33m
COLOR_BLUE = \033[34m
COLOR_RED = \033[31m

.PHONY: help restore build run clean publish \
        docker-up docker-down docker-restart docker-init docker-logs docker-logs-tail \
        docker-exec docker-status docker-clean docker-rebuild docker-test-connection \
        docker-clean-db \
        use-adonet use-efcore ef-init ef-init-existing ef-update ef-migrate ef-migrations-list \
        ef-migrations-remove show-data-access

# Default target: show help
help:
	@echo "$(COLOR_BLUE)╔════════════════════════════════════════════════════════════╗$(COLOR_RESET)"
	@echo "$(COLOR_BLUE)║         AnimalZoo Project Management Commands              ║$(COLOR_RESET)"
	@echo "$(COLOR_BLUE)╚════════════════════════════════════════════════════════════╝$(COLOR_RESET)"
	@echo ""
	@echo "$(COLOR_GREEN).NET Build Commands:$(COLOR_RESET)"
	@echo "  make restore         - Restore NuGet packages"
	@echo "  make build           - Build the solution (Debug)"
	@echo "  make run             - Build and run the application"
	@echo "  make clean           - Clean build artifacts"
	@echo ""
	@echo "$(COLOR_GREEN)Docker Management Commands:$(COLOR_RESET)"
	@echo "  make docker-up       - Start SQL Server container with Docker Compose"
	@echo "  make docker-down     - Stop and remove SQL Server container"
	@echo "  make docker-restart  - Restart SQL Server container"
	@echo "  make docker-status   - Show container status and health"
	@echo "  make docker-logs     - Show SQL Server logs (follow mode)"
	@echo "  make docker-logs-tail - Show last 50 lines of logs"
	@echo ""
	@echo "$(COLOR_GREEN)Database Operations (ADO.NET):$(COLOR_RESET)"
	@echo "  make docker-init     - Initialize database schema with SQL script"
	@echo "  make docker-exec     - Open SQL command line (sqlcmd) in container"
	@echo "  make docker-test-connection - Test database connection"
	@echo "  make docker-clean-db - Clean all data from Animals and Enclosures tables"
	@echo ""
	@echo "$(COLOR_GREEN)Data Access Configuration:$(COLOR_RESET)"
	@echo "  make use-adonet      - Switch to ADO.NET repositories (default)"
	@echo "  make use-efcore      - Switch to Entity Framework Core repositories"
	@echo "  make show-data-access - Show current data access configuration"
	@echo ""
	@echo "$(COLOR_GREEN)Entity Framework Core Commands:$(COLOR_RESET)"
	@echo "  make ef-init         - Apply EF Core migrations to database (first time setup)"
	@echo "  make ef-init-existing - Mark existing ADO.NET database as migrated"
	@echo "  make ef-update       - Apply pending EF Core migrations"
	@echo "  make ef-migrate      - Create a new migration (NAME=MigrationName)"
	@echo "  make ef-migrations-list - List all migrations"
	@echo "  make ef-migrations-remove - Remove last migration (if not applied)"
	@echo ""
	@echo "$(COLOR_GREEN)Maintenance:$(COLOR_RESET)"
	@echo "  make docker-clean    - Remove container and volumes (deletes all data!)"
	@echo "  make docker-rebuild  - Clean and start fresh"
	@echo ""
	@echo "$(COLOR_YELLOW)Configuration:$(COLOR_RESET)"
	@echo "  Container: $(DOCKER_NAME)"
	@echo "  Port:      $(DB_PORT)"
	@echo "  Database:  $(DB_NAME)"
	@echo "  Platform:  $(PLATFORM) (Apple Silicon compatible)"
	@echo ""
	@echo "$(COLOR_BLUE)Quick Start (ADO.NET - default):$(COLOR_RESET)"
	@echo "  1. make docker-up    # Start SQL Server"
	@echo "  2. make docker-init  # Initialize database with SQL script"
	@echo "  3. make run          # Build and run the app"
	@echo ""
	@echo "$(COLOR_BLUE)Quick Start (Entity Framework Core):$(COLOR_RESET)"
	@echo "  1. make docker-up    # Start SQL Server"
	@echo "  2. make use-efcore   # Switch to EF Core"
	@echo "  3. make ef-init      # Apply EF Core migrations"
	@echo "  4. make run          # Build and run the app"

# .NET restore packages
restore:
	@echo "$(COLOR_BLUE)Restoring NuGet packages...$(COLOR_RESET)"
	@dotnet restore $(SOLUTION)
	@echo "$(COLOR_GREEN)✓ Packages restored$(COLOR_RESET)"

# .NET build solution
build: restore
	@echo "$(COLOR_BLUE)Building solution...$(COLOR_RESET)"
	@dotnet build $(SOLUTION) -c Debug
	@echo "$(COLOR_GREEN)✓ Build complete$(COLOR_RESET)"

# .NET run application
run: build
	@echo "$(COLOR_BLUE)Starting AnimalZoo application...$(COLOR_RESET)"
	@dotnet run --project $(APP) -f net9.0

# .NET clean build artifacts
clean:
	@echo "$(COLOR_YELLOW)Cleaning build artifacts...$(COLOR_RESET)"
	@dotnet clean $(SOLUTION)
	@rm -rf **/bin **/obj
	@echo "$(COLOR_GREEN)✓ Clean complete$(COLOR_RESET)"

# Start SQL Server container using Docker Compose
docker-up:
	@echo "$(COLOR_GREEN)Starting SQL Server container...$(COLOR_RESET)"
	@if [ ! -f .env ]; then \
		echo "$(COLOR_YELLOW)Creating .env from .env.example...$(COLOR_RESET)"; \
		cp .env.example .env; \
	fi
	@docker compose -f $(COMPOSE_FILE) up -d
	@echo "$(COLOR_GREEN)Waiting for SQL Server to be ready...$(COLOR_RESET)"
	@sleep 10
	@for i in 1 2 3 4 5; do \
		if docker logs $(DOCKER_NAME) 2>&1 | grep -q "SQL Server is now ready for client connections"; then \
			echo "$(COLOR_GREEN)✓ SQL Server is ready!$(COLOR_RESET)"; \
			break; \
		else \
			echo "$(COLOR_YELLOW)Waiting... ($$i/5)$(COLOR_RESET)"; \
			sleep 5; \
		fi; \
	done

# Stop and remove SQL Server container
docker-down:
	@echo "$(COLOR_YELLOW)Stopping SQL Server container...$(COLOR_RESET)"
	@docker compose -f $(COMPOSE_FILE) down
	@echo "$(COLOR_GREEN)✓ Container stopped$(COLOR_RESET)"

# Restart SQL Server container
docker-restart:
	@echo "$(COLOR_YELLOW)Restarting SQL Server container...$(COLOR_RESET)"
	@docker compose -f $(COMPOSE_FILE) restart
	@echo "$(COLOR_GREEN)Waiting for SQL Server to be ready...$(COLOR_RESET)"
	@sleep 5
	@echo "$(COLOR_GREEN)✓ Container restarted$(COLOR_RESET)"

# Initialize database schema by running the SQL script
docker-init:
	@echo "$(COLOR_GREEN)Initializing database schema...$(COLOR_RESET)"
	@if ! docker ps --filter "name=$(DOCKER_NAME)" --filter "status=running" | grep -q $(DOCKER_NAME); then \
		echo "$(COLOR_RED)Error: Container is not running. Run 'make docker-up' first.$(COLOR_RESET)"; \
		exit 1; \
	fi
	@echo "$(COLOR_BLUE)Copying SQL script to container...$(COLOR_RESET)"
	@docker cp $(INIT_SCRIPT) $(DOCKER_NAME):/$(INIT_SCRIPT)
	@echo "$(COLOR_BLUE)Executing initialization script...$(COLOR_RESET)"
	@docker exec $(DOCKER_NAME) /opt/mssql-tools18/bin/sqlcmd \
		-S localhost -U sa -P "$(SA_PASSWORD)" -C \
		-i /$(INIT_SCRIPT)
	@echo "$(COLOR_GREEN)✓ Database initialized successfully$(COLOR_RESET)"

# Show SQL Server logs (follow mode)
docker-logs:
	@echo "$(COLOR_BLUE)Showing SQL Server logs (Ctrl+C to exit)...$(COLOR_RESET)"
	@docker logs -f $(DOCKER_NAME)

# Show last 50 lines of SQL Server logs
docker-logs-tail:
	@docker logs --tail 50 $(DOCKER_NAME)

# Open interactive SQL command line
docker-exec:
	@echo "$(COLOR_BLUE)Opening SQL command line...$(COLOR_RESET)"
	@echo "$(COLOR_YELLOW)Tip: Type 'SELECT @@VERSION;' and then 'GO' to test$(COLOR_RESET)"
	@docker exec -it $(DOCKER_NAME) /opt/mssql-tools18/bin/sqlcmd \
		-S localhost -U sa -P "$(SA_PASSWORD)" -C

# Show container status and health information
docker-status:
	@echo "$(COLOR_BLUE)=== Container Status ===$(COLOR_RESET)"
	@if docker ps --filter "name=$(DOCKER_NAME)" --filter "status=running" | grep -q $(DOCKER_NAME); then \
		echo "$(COLOR_GREEN)Status: RUNNING ✓$(COLOR_RESET)"; \
		docker ps --filter "name=$(DOCKER_NAME)" --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}"; \
	elif docker ps -a --filter "name=$(DOCKER_NAME)" | grep -q $(DOCKER_NAME); then \
		echo "$(COLOR_RED)Status: STOPPED$(COLOR_RESET)"; \
		docker ps -a --filter "name=$(DOCKER_NAME)" --format "table {{.Names}}\t{{.Status}}"; \
	else \
		echo "$(COLOR_RED)Status: NOT FOUND$(COLOR_RESET)"; \
		echo "Run 'make docker-up' to create the container"; \
	fi
	@echo ""
	@echo "$(COLOR_BLUE)=== Docker Compose Services ===$(COLOR_RESET)"
	@docker compose -f $(COMPOSE_FILE) ps || true

# Test database connection
docker-test-connection:
	@echo "$(COLOR_BLUE)Testing database connection...$(COLOR_RESET)"
	@if ! docker ps --filter "name=$(DOCKER_NAME)" --filter "status=running" | grep -q $(DOCKER_NAME); then \
		echo "$(COLOR_RED)Error: Container is not running$(COLOR_RESET)"; \
		exit 1; \
	fi
	@docker exec $(DOCKER_NAME) /opt/mssql-tools18/bin/sqlcmd \
		-S localhost -U sa -P "$(SA_PASSWORD)" -C \
		-Q "SELECT @@VERSION AS 'SQL Server Version';" \
		&& echo "$(COLOR_GREEN)✓ Connection successful$(COLOR_RESET)" \
		|| echo "$(COLOR_RED)✗ Connection failed$(COLOR_RESET)"

# Remove container and volumes (WARNING: deletes all data)
docker-clean:
	@echo "$(COLOR_RED)WARNING: This will delete all database data!$(COLOR_RESET)"
	@read -p "Are you sure? [y/N] " -n 1 -r; \
	echo; \
	if [[ $$REPLY =~ ^[Yy]$$ ]]; then \
		echo "$(COLOR_YELLOW)Removing container and volumes...$(COLOR_RESET)"; \
		docker compose -f $(COMPOSE_FILE) down -v; \
		echo "$(COLOR_GREEN)✓ Cleanup complete$(COLOR_RESET)"; \
	else \
		echo "$(COLOR_BLUE)Cancelled$(COLOR_RESET)"; \
	fi

# Clean and rebuild from scratch
docker-rebuild: docker-clean docker-up docker-init
	@echo "$(COLOR_GREEN)✓ Rebuild complete$(COLOR_RESET)"

# Clean all data from database tables (keeps schema)
docker-clean-db:
	@echo "$(COLOR_RED)WARNING: This will delete all animals and enclosure data!$(COLOR_RESET)"
	@read -p "Are you sure? [y/N] " -n 1 -r; \
	echo; \
	if [[ $$REPLY =~ ^[Yy]$$ ]]; then \
		echo "$(COLOR_YELLOW)Cleaning database tables...$(COLOR_RESET)"; \
		if ! docker ps --filter "name=$(DOCKER_NAME)" --filter "status=running" | grep -q $(DOCKER_NAME); then \
			echo "$(COLOR_RED)Error: Container is not running$(COLOR_RESET)"; \
			exit 1; \
		fi; \
		docker exec $(DOCKER_NAME) /opt/mssql-tools18/bin/sqlcmd \
			-S localhost -U sa -P "$(SA_PASSWORD)" -C \
			-Q "USE $(DB_NAME); DELETE FROM Enclosures; DELETE FROM Animals; PRINT 'Database cleaned successfully';" \
			&& echo "$(COLOR_GREEN)✓ Database cleaned$(COLOR_RESET)" \
			|| echo "$(COLOR_RED)✗ Failed to clean database$(COLOR_RESET)"; \
	else \
		echo "$(COLOR_BLUE)Cancelled$(COLOR_RESET)"; \
	fi

# Switch to ADO.NET repositories
use-adonet:
	@echo "$(COLOR_BLUE)Switching to ADO.NET repositories...$(COLOR_RESET)"
	@if [ "$$(uname)" = "Darwin" ]; then \
		sed -i '' 's/"RepositoryType": "EfCore"/"RepositoryType": "AdoNet"/' $(APPSETTINGS); \
	else \
		sed -i 's/"RepositoryType": "EfCore"/"RepositoryType": "AdoNet"/' $(APPSETTINGS); \
	fi
	@echo "$(COLOR_GREEN)✓ Switched to ADO.NET$(COLOR_RESET)"
	@echo "$(COLOR_YELLOW)Note: Use 'make docker-init' to initialize database with SQL script$(COLOR_RESET)"

# Switch to Entity Framework Core repositories
use-efcore:
	@echo "$(COLOR_BLUE)Switching to Entity Framework Core repositories...$(COLOR_RESET)"
	@if [ "$$(uname)" = "Darwin" ]; then \
		sed -i '' 's/"RepositoryType": "AdoNet"/"RepositoryType": "EfCore"/' $(APPSETTINGS); \
	else \
		sed -i 's/"RepositoryType": "AdoNet"/"RepositoryType": "EfCore"/' $(APPSETTINGS); \
	fi
	@echo "$(COLOR_GREEN)✓ Switched to Entity Framework Core$(COLOR_RESET)"
	@echo ""
	@echo "$(COLOR_YELLOW)Next steps:$(COLOR_RESET)"
	@echo "  • If database is empty:        make ef-init"
	@echo "  • If database was created with ADO.NET: make ef-init-existing"

# Show current data access configuration
show-data-access:
	@echo "$(COLOR_BLUE)Current Data Access Configuration:$(COLOR_RESET)"
	@grep -A 2 '"DataAccess"' $(APPSETTINGS) | grep '"RepositoryType"' | sed 's/.*"RepositoryType": "\(.*\)".*/  Type: \1/' || echo "  Type: Not configured"

# Apply EF Core migrations (first time setup)
ef-init:
	@echo "$(COLOR_GREEN)Applying Entity Framework Core migrations...$(COLOR_RESET)"
	@if ! command -v dotnet-ef &> /dev/null; then \
		echo "$(COLOR_YELLOW)Installing dotnet-ef tool...$(COLOR_RESET)"; \
		dotnet tool install --global dotnet-ef; \
	fi
	@if ! docker ps --filter "name=$(DOCKER_NAME)" --filter "status=running" | grep -q $(DOCKER_NAME); then \
		echo "$(COLOR_RED)Error: Container is not running. Run 'make docker-up' first.$(COLOR_RESET)"; \
		exit 1; \
	fi
	@cd AnimalZoo.App && dotnet ef database update 2>&1 | tee /tmp/ef-output.txt; \
	if grep -q "already an object named" /tmp/ef-output.txt; then \
		echo ""; \
		echo "$(COLOR_YELLOW)⚠ Database was created with ADO.NET script$(COLOR_RESET)"; \
		echo "$(COLOR_YELLOW)Run: make ef-init-existing$(COLOR_RESET)"; \
		rm /tmp/ef-output.txt; \
		exit 1; \
	fi
	@rm -f /tmp/ef-output.txt
	@echo "$(COLOR_GREEN)✓ EF Core migrations applied successfully$(COLOR_RESET)"

# Mark existing ADO.NET database as migrated (for switching from ADO.NET to EF Core)
ef-init-existing:
	@echo "$(COLOR_BLUE)Marking existing database as migrated...$(COLOR_RESET)"
	@if ! command -v dotnet-ef &> /dev/null; then \
		echo "$(COLOR_YELLOW)Installing dotnet-ef tool...$(COLOR_RESET)"; \
		dotnet tool install --global dotnet-ef; \
	fi
	@if ! docker ps --filter "name=$(DOCKER_NAME)" --filter "status=running" | grep -q $(DOCKER_NAME); then \
		echo "$(COLOR_RED)Error: Container is not running. Run 'make docker-up' first.$(COLOR_RESET)"; \
		exit 1; \
	fi
	@echo "$(COLOR_YELLOW)This tells EF Core that InitialCreate migration is already applied.$(COLOR_RESET)"
	@docker exec $(DOCKER_NAME) /opt/mssql-tools18/bin/sqlcmd \
		-S localhost -U sa -P "$(SA_PASSWORD)" -C \
		-Q "USE $(DB_NAME); \
		    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = '__EFMigrationsHistory') \
		    BEGIN \
		        CREATE TABLE __EFMigrationsHistory ( \
		            MigrationId nvarchar(150) NOT NULL PRIMARY KEY, \
		            ProductVersion nvarchar(32) NOT NULL \
		        ); \
		    END; \
		    IF NOT EXISTS (SELECT * FROM __EFMigrationsHistory WHERE MigrationId = '20251110134623_InitialCreate') \
		    BEGIN \
		        INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion) \
		        VALUES ('20251110134623_InitialCreate', '9.0.0'); \
		        PRINT 'Marked InitialCreate as applied'; \
		    END \
		    ELSE \
		    BEGIN \
		        PRINT 'Migration already marked as applied'; \
		    END" > /dev/null 2>&1
	@echo "$(COLOR_GREEN)✓ Database marked as migrated$(COLOR_RESET)"
	@echo "$(COLOR_GREEN)You can now use EF Core with this database$(COLOR_RESET)"

# Apply pending EF Core migrations (same as ef-init)
ef-update: ef-init

# Create a new EF Core migration
ef-migrate:
	@if [ -z "$(NAME)" ]; then \
		echo "$(COLOR_RED)Error: Migration name not specified$(COLOR_RESET)"; \
		echo "Usage: make ef-migrate NAME=MigrationName"; \
		exit 1; \
	fi
	@echo "$(COLOR_BLUE)Creating migration: $(NAME)$(COLOR_RESET)"
	@if ! command -v dotnet-ef &> /dev/null; then \
		echo "$(COLOR_YELLOW)Installing dotnet-ef tool...$(COLOR_RESET)"; \
		dotnet tool install --global dotnet-ef; \
	fi
	@cd AnimalZoo.App && dotnet ef migrations add $(NAME) --output-dir Data/Migrations
	@echo "$(COLOR_GREEN)✓ Migration created successfully$(COLOR_RESET)"

# List all EF Core migrations
ef-migrations-list:
	@echo "$(COLOR_BLUE)Entity Framework Core Migrations:$(COLOR_RESET)"
	@if ! command -v dotnet-ef &> /dev/null; then \
		echo "$(COLOR_RED)Error: dotnet-ef tool not installed$(COLOR_RESET)"; \
		echo "Run: dotnet tool install --global dotnet-ef"; \
		exit 1; \
	fi
	@cd AnimalZoo.App && dotnet ef migrations list

# Remove last EF Core migration (if not applied)
ef-migrations-remove:
	@echo "$(COLOR_YELLOW)Removing last migration...$(COLOR_RESET)"
	@if ! command -v dotnet-ef &> /dev/null; then \
		echo "$(COLOR_RED)Error: dotnet-ef tool not installed$(COLOR_RESET)"; \
		exit 1; \
	fi
	@cd AnimalZoo.App && dotnet ef migrations remove
	@echo "$(COLOR_GREEN)✓ Last migration removed$(COLOR_RESET)"
