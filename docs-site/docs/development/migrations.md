---
id: migrations
title: Database Migrations
---

Entity Framework Core migration management for the URL Shortener database schema.

## Automated Migration Scripts (Recommended)

The project includes automated scripts that handle Docker-based migrations with proper error checking and logging.

### Linux/macOS
```bash
./scripts/migrate.sh
```

### Windows PowerShell
```bash
# View help and options
.\scripts\migrate.ps1 -Help

# Run migration with default settings
.\scripts\migrate.ps1

# Run with custom connection string
.\scripts\migrate.ps1 -ConnectionString "Host=localhost;Port=5432;Database=urlshortener;Username=postgres;Password=mypassword;"
```

### What the Scripts Do

1. **✅ Prerequisites Check**
   - Verify Docker is running
   - Ensure PostgreSQL container is healthy
   - Validate Docker network exists (`urlshortener_network`)

2. **🏗️ Build Migration Container**
   - Create temporary Docker image with EF Core tools
   - Include all necessary dependencies and connection strings

3. **🚀 Execute Migrations**
   - Run `dotnet ef database update` inside container
   - Apply all pending migrations to the database

4. **✅ Validation**
   - Confirm migrations completed successfully
   - Provide clear success/error messages

### Script Output Example

```bash
$ ./scripts/migrate.sh

🔍 Checking prerequisites...
   ✅ Docker is running
   ✅ PostgreSQL container is healthy
   ✅ Network 'urlshortener_network' exists

🏗️ Building migration container...
   ✅ Migration container built successfully

🚀 Applying database migrations...
   ℹ️ Applying migration 'InitialCreate'
   ℹ️ Applying migration 'AddClickCount'
   ℹ️ Applying migration 'AddTitleAndDescription'
   ✅ All migrations applied successfully

🎉 Database migration completed!
```

## Manual Migration (Advanced Users)

### Prerequisites
Ensure you have the Entity Framework tools installed:

```bash
# Install globally (one-time setup)
dotnet tool install --global dotnet-ef

# Or update if already installed
dotnet tool update --global dotnet-ef

# Verify installation
dotnet ef --version
```

### Create New Migration
When you modify entities or add new ones:

```bash
# Add a new migration
dotnet ef migrations add MigrationName --project src/Infrastructure --startup-project src/API

# Example: Adding a new property
dotnet ef migrations add AddUserIdToUrlMapping --project src/Infrastructure --startup-project src/API
```

### Apply Migrations
```bash
# Apply all pending migrations
dotnet ef database update --project src/Infrastructure --startup-project src/API

# Apply to specific migration
dotnet ef database update MigrationName --project src/Infrastructure --startup-project src/API

# Apply with custom connection string
dotnet ef database update --project src/Infrastructure --startup-project src/API --connection "Host=localhost;Port=5432;Database=urlshortener;Username=postgres;Password=postgres"
```

### View Migration Status
```bash
# List all migrations and their status
dotnet ef migrations list --project src/Infrastructure --startup-project src/API

# View migration history
dotnet ef database update --project src/Infrastructure --startup-project src/API --verbose
```

## Docker-Based Migration (Manual)

For containerized environments or CI/CD pipelines:

### Build Migration Container
```bash
# Build the migration container
docker build -f Dockerfile.migration -t migration-runner .
```

### Run Migrations
```bash
# Run migrations with default connection
docker run --rm --network urlshortener_network migration-runner

# Run with custom connection string
docker run --rm --network urlshortener_network migration-runner \
  --connection "Host=postgres;Port=5432;Database=urlshortener;Username=postgres;Password=YourPassword;"
```

### Migration Container Details
The `Dockerfile.migration` creates a specialized container that:
- Includes .NET SDK and EF Core tools
- Copies the source code and configurations
- Runs migrations in an isolated environment
- Connects to the database via Docker network

## Migration History

### Current Migrations

| Migration | Date | Description |
|-----------|------|-------------|
| `InitialCreate` | 2024-01-10 | Initial database schema with UrlMapping table |
| `AddClickCount` | 2024-01-12 | Added click tracking functionality |
| `AddTitleDescription` | 2024-01-15 | Added metadata fields for URLs |
| `AddIsActiveFlag` | 2024-01-18 | Added soft delete capability |

### Schema Overview
```sql
-- Current schema after all migrations
CREATE TABLE "UrlMappings" (
    "Id" SERIAL PRIMARY KEY,
    "OriginalUrl" TEXT NOT NULL,
    "ShortCode" VARCHAR(8) NOT NULL UNIQUE,
    "ClickCount" INTEGER NOT NULL DEFAULT 0,
    "ExpiresAt" TIMESTAMP WITH TIME ZONE NULL,
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE,
    "Title" TEXT NULL,
    "Description" TEXT NULL,
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "UpdatedAt" TIMESTAMP WITH TIME ZONE NULL
);

-- Indexes for performance
CREATE INDEX "IX_UrlMappings_ShortCode" ON "UrlMappings" ("ShortCode");
CREATE INDEX "IX_UrlMappings_IsActive" ON "UrlMappings" ("IsActive");
CREATE INDEX "IX_UrlMappings_ClickCount" ON "UrlMappings" ("ClickCount" DESC);
```

## Troubleshooting Migrations

### Common Issues

**1. Migration Scripts Fail**
```bash
# Check Docker status
docker ps
docker network ls | grep urlshortener

# View PostgreSQL logs
docker logs postgres

# Test database connectivity
docker exec -it postgres psql -U postgres -d urlshortener -c "SELECT 1;"
```

**2. EF Tools Not Found**
```bash
# Install/update EF tools
dotnet tool install --global dotnet-ef
dotnet tool update --global dotnet-ef

# Add to PATH if necessary (Linux/macOS)
export PATH="$PATH:$HOME/.dotnet/tools"
```

**3. Connection String Issues**
```bash
# Verify connection string format
# Correct: "Host=localhost;Port=5432;Database=urlshortener;Username=postgres;Password=postgres"
# Check for typos in host, database name, credentials

# Test connection manually
psql -h localhost -p 5432 -U postgres -d urlshortener
```

**4. Migration Already Applied**
```bash
# Check migration status
dotnet ef migrations list --project src/Infrastructure --startup-project src/API

# Remove last migration if needed
dotnet ef migrations remove --project src/Infrastructure --startup-project src/API
```

**5. Database Doesn't Exist**
```bash
# Create database manually
docker exec -it postgres psql -U postgres -c "CREATE DATABASE urlshortener;"

# Or let migrations create it automatically
dotnet ef database update --project src/Infrastructure --startup-project src/API
```

### Reset Database (Development Only)

**⚠️ WARNING: This will destroy all data!**

```bash
# Drop and recreate database
dotnet ef database drop --project src/Infrastructure --startup-project src/API --force
dotnet ef database update --project src/Infrastructure --startup-project src/API

# Using scripts (safer)
./scripts/migrate.sh --reset
```

## Production Migrations

### CI/CD Pipeline Integration
```yaml
# Example GitHub Actions step
- name: Apply Database Migrations
  run: |
    docker build -f Dockerfile.migration -t migration-runner .
    docker run --rm --network production_network migration-runner \
      --connection "${{ secrets.DATABASE_CONNECTION_STRING }}"
```

### Blue/Green Deployment
```bash
# 1. Apply migrations to new database
dotnet ef database update --connection $NEW_DB_CONNECTION

# 2. Verify migration success
dotnet ef database update --dry-run --connection $NEW_DB_CONNECTION

# 3. Switch application to new database
# 4. Keep old database for rollback if needed
```

### Rollback Strategy
```bash
# Rollback to specific migration
dotnet ef database update PreviousMigrationName --project src/Infrastructure --startup-project src/API

# View rollback SQL
dotnet ef migrations script CurrentMigration PreviousMigration --project src/Infrastructure --startup-project src/API
```

## Best Practices

### ✅ Do's
1. **Always backup before migrations** in production
2. **Test migrations** on staging environment first
3. **Use descriptive migration names** (`AddUserIdToUrlMapping` vs `Migration1`)
4. **Keep migrations small** and focused on single changes
5. **Review generated SQL** before applying to production

### ❌ Don'ts
1. **Don't modify existing migrations** once applied to production
2. **Don't delete migration files** from the project
3. **Don't run migrations directly on production** without testing
4. **Don't ignore migration warnings** or errors
5. **Don't mix data migrations with schema changes**

### Migration Naming Conventions
```bash
# Good examples
dotnet ef migrations add InitialCreate
dotnet ef migrations add AddClickCountToUrlMapping
dotnet ef migrations add CreateIndexOnShortCode
dotnet ef migrations add AddUserAuthenticationTables

# Avoid
dotnet ef migrations add Update1
dotnet ef migrations add FixStuff
dotnet ef migrations add Changes
```
