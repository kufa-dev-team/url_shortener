---
id: local-setup
title: Local Development Setup
---

Complete guide for setting up the URL Shortener for local development with all dependencies and tools.

## Prerequisites

### Required Software
- **.NET 9 SDK** - [Download here](https://dotnet.microsoft.com/download)
- **Docker Desktop** (Windows/Mac) or **Docker Engine** (Linux) - [Download here](https://docker.com/products/docker-desktop)
- **Git** - For cloning the repository

### Optional Tools
- **Visual Studio 2022** or **VS Code** with C# extension
- **JetBrains Rider**
- **Azure Data Studio** or **pgAdmin** for database management

### Verify Prerequisites
```bash
# Check .NET version
dotnet --version
# Should show 9.0.x or later

# Check Docker
docker --version
docker-compose --version

# Check Git
git --version
```

## Quick Start (Recommended)

### 1. Clone Repository
```bash
git clone https://github.com/kufa-dev-team/url_shortener.git
cd url_shortener
```

### 2. Setup Environment Configuration
```bash
# Copy environment template
cd docker
cp .env.example .env

# Edit .env file with your preferences (optional)
# Default values work for local development
```

### 3. Start Infrastructure Services
```bash
# Start PostgreSQL, Redis, and admin tools
docker-compose -f docker-compose.dev.yml up -d

# Verify all services are running
docker-compose -f docker-compose.dev.yml ps
```

**Services Started:**
- **PostgreSQL** (port 5432) - Main database
- **Redis** (port 6379) - Caching layer  
- **Supabase Studio** (port 8080) - Database management UI
- **Redis Commander** (port 8081) - Redis management UI

### 4. Apply Database Migrations
```bash
# Navigate back to project root
cd ..

# Run migration script (recommended)
./scripts/migrate.sh          # Linux/macOS
.\scripts\migrate.ps1         # Windows PowerShell

# OR manual migration
dotnet ef database update --project src/Infrastructure --startup-project src/API
```

### 5. Build and Run Application
```bash
# Build the solution
dotnet build

# Run the API
dotnet run --project src/API

# Application will start at:
# HTTP: http://localhost:5135
# HTTPS: https://localhost:7218
```

### 6. Verify Installation
```bash
# Test API health
curl http://localhost:5135/health

# Create a test short URL
curl -X POST http://localhost:5135/UrlShortener \
  -H "Content-Type: application/json" \
  -d '{"originalUrl": "https://github.com/microsoft/dotnet"}'

# Test redirect (use the shortCode from response)
curl -L http://localhost:5135/abc12345
```

## Development Services Access

| Service | URL | Credentials | Purpose |
|---------|-----|-------------|---------|
| **API** | http://localhost:5135 | - | Main application |
| **Swagger UI** | http://localhost:5135/swagger | - | API documentation |
| **Supabase Studio** | http://localhost:8080 | Direct DB connection | PostgreSQL management |
| **Redis Commander** | http://localhost:8081 | admin/admin | Redis cache management |
| **Health Checks** | http://localhost:5135/health | - | System status |
| **Metrics** | http://localhost:5135/metrics | - | Prometheus metrics |

## Manual Setup (Alternative)

If you prefer to install databases locally without Docker:

### 1. Install PostgreSQL
```bash
# macOS (using Homebrew)
brew install postgresql@16
brew services start postgresql

# Ubuntu/Debian
sudo apt update
sudo apt install postgresql-16 postgresql-client-16

# Windows
# Download from https://www.postgresql.org/download/windows/
```

### 2. Install Redis
```bash
# macOS
brew install redis
brew services start redis

# Ubuntu/Debian
sudo apt install redis-server

# Windows
# Download from https://github.com/microsoftarchive/redis/releases
```

### 3. Create Database
```sql
-- Connect to PostgreSQL as superuser
psql -U postgres

-- Create database and user
CREATE DATABASE urlshortener;
CREATE USER urluser WITH PASSWORD 'urlpassword';
GRANT ALL PRIVILEGES ON DATABASE urlshortener TO urluser;
```

### 4. Update Configuration
Edit `src/API/appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=urlshortener;Username=urluser;Password=urlpassword"
  },
  "Redis": {
    "ConnectionString": "localhost:6379",
    "IsEnabled": true
  }
}
```

## Development Workflow

### 1. Code Changes
```bash
# Make your changes in src/ folder
# Build to check for errors
dotnet build

# Run tests
dotnet test

# Run the application
dotnet run --project src/API
```

### 2. Database Migrations
When you modify entities:

```bash
# Add new migration
dotnet ef migrations add YourMigrationName --project src/Infrastructure --startup-project src/API

# Apply migration
dotnet ef database update --project src/Infrastructure --startup-project src/API

# OR use migration scripts
./scripts/migrate.sh
```

### 3. Hot Reload (Development)
The API supports hot reload for development:

```bash
# Run with hot reload enabled
dotnet watch --project src/API

# Changes to C# files will automatically rebuild and restart
```

### 4. Debug Configuration
For Visual Studio/VS Code debugging:

**launch.json** (VS Code):
```json
{
  "version": "0.2.0",
  "configurations": [
    {
      "name": ".NET Core Launch (web)",
      "type": "coreclr",
      "request": "launch",
      "preLaunchTask": "build",
      "program": "${workspaceFolder}/src/API/bin/Debug/net9.0/API.dll",
      "args": [],
      "cwd": "${workspaceFolder}/src/API",
      "stopAtEntry": false,
      "serverReadyAction": {
        "action": "openExternally",
        "pattern": "\\bNow listening on:\\s+(https?://\\S+)"
      },
      "env": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  ]
}
```

## Troubleshooting

### Common Issues

**1. Port Already in Use**
```bash
# Check what's using port 5432 (PostgreSQL)
lsof -i :5432

# Kill process if needed
kill -9 <PID>
```

**2. Docker Services Won't Start**
```bash
# Check Docker is running
docker info

# View service logs
docker-compose -f docker/docker-compose.dev.yml logs postgres
docker-compose -f docker/docker-compose.dev.yml logs redis
```

**3. Database Connection Failed**
```bash
# Test PostgreSQL connection
docker exec -it postgres psql -U postgres -d urlshortener -c "SELECT 1;"

# Check connection string in appsettings.json
```

**4. Migration Errors**
```bash
# Reset database (WARNING: destroys data)
dotnet ef database drop --project src/Infrastructure --startup-project src/API
dotnet ef database update --project src/Infrastructure --startup-project src/API
```

**5. Redis Connection Issues**
```bash
# Test Redis connection
docker exec -it redis redis-cli ping
# Should return: PONG
```

**6. Build Errors**
```bash
# Clean build
dotnet clean
dotnet build

# Restore packages
dotnet restore
```

### Performance Tips

**1. Use Release Mode for Performance Testing**
```bash
dotnet run --project src/API --configuration Release
```

**2. Enable SQL Query Logging (Development Only)**
Add to `appsettings.Development.json`:
```json
{
  "Logging": {
    "LogLevel": {
      "Microsoft.EntityFrameworkCore.Database.Command": "Information"
    }
  }
}
```

**3. Monitor Resource Usage**
```bash
# Check Docker resource usage
docker stats

# Monitor .NET process
dotnet-counters monitor --process-id <PID>
```

## IDE Configuration

### Visual Studio 2022
1. Open `UrlShortener.sln`
2. Set `API` as startup project
3. Configure connection strings in `appsettings.Development.json`
4. Press F5 to run with debugging

### VS Code
1. Install C# extension
2. Open project folder
3. Use Ctrl+Shift+P → ".NET: Generate Assets for Build and Debug"
4. Press F5 to start debugging

### JetBrains Rider
1. Open `UrlShortener.sln`
2. Configure run configuration for `API` project
3. Set environment to `Development`
4. Run/Debug with Ctrl+F5/F5

## Next Steps

Once your local environment is running:

1. **Explore the API** - Visit http://localhost:5135/swagger
2. **Check the database** - Use Supabase Studio at http://localhost:8080
3. **Monitor cache** - Use Redis Commander at http://localhost:8081
4. **Review the code** - Start with `src/API/Controllers/UrlShortenerController.cs`
5. **Run tests** - Execute `dotnet test` to run the test suite
6. **Check monitoring** - Visit http://localhost:5135/health and http://localhost:5135/metrics

For production deployment, see the [Docker Compose](docker-compose.md) documentation.
