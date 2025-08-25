---
id: getting-started
title: Getting Started
slug: /getting-started
---

Get the URL Shortener up and running quickly with Docker Compose for the complete development environment.

## Prerequisites

- **.NET 9 SDK** - [Download here](https://dotnet.microsoft.com/download/dotnet/9.0)
- **Docker Desktop** (macOS/Windows) or **Docker Engine** (Linux) - [Download here](https://docker.com/products/docker-desktop)
- **Git** - For cloning the repository

Verify your setup:
```bash
dotnet --version    # Should show 9.0.x or later
docker --version    # Should show Docker version
git --version       # Should show Git version
```

## Quick Start with Docker

### 1. Clone and Navigate
```bash
git clone https://github.com/kufa-dev-team/url_shortener.git
cd url_shortener
```

### 2. Start Infrastructure Services
```bash
# Navigate to Docker configuration
cd docker

# Copy environment configuration (edit if needed)
cp .env.example .env

# Start all infrastructure services
docker-compose -f docker-compose.dev.yml up -d

# Verify services are running
docker-compose -f docker-compose.dev.yml ps
```

**Services Started:**
| Service | Port | URL | Purpose |
|---------|------|-----|---------|
| PostgreSQL | 5432 | - | Main database |
| Redis | 6379 | - | Caching layer |
| Supabase Studio | 8080 | http://localhost:8080 | Database management UI |
| Redis Commander | 8081 | http://localhost:8081 | Redis management UI |

### 3. Apply Database Migrations
```bash
# Navigate back to project root
cd ..

# Run automated migration script (recommended)
./scripts/migrate.sh          # Linux/macOS
.\scripts\migrate.ps1         # Windows PowerShell
```

The migration script will:
- ✅ Check prerequisites (Docker, PostgreSQL health)
- 🏗️ Build migration container with EF Core tools
- 🚀 Apply all database migrations
- ✅ Confirm successful completion

### 4. Build and Run the API
```bash
# Build the solution
dotnet build

# Run the API
dotnet run --project src/API
```

The API will start at:
- **HTTP**: http://localhost:5135
- **HTTPS**: https://localhost:7218
- **Swagger UI**: http://localhost:5135/swagger

## Verify Your Setup

### 1. Health Check
```bash
curl http://localhost:5135/health
```
Expected response:
```json
{
  "status": "Healthy",
  "entries": {
    "database": { "status": "Healthy" },
    "redis-cache": { "status": "Healthy" }
  }
}
```

### 2. Create Your First Short URL
```bash
curl -X POST http://localhost:5135/UrlShortener \
  -H "Content-Type: application/json" \
  -d '{
    "originalUrl": "https://github.com/microsoft/dotnet",
    "title": ".NET Repository",
    "description": "Official Microsoft .NET repository"
  }'
```

Expected response:
```json
{
  "id": 1,
  "shortCode": "abc12345",
  "originalUrl": "https://github.com/microsoft/dotnet",
  "shortUrl": "http://localhost:5135/abc12345",
  "title": ".NET Repository",
  "description": "Official Microsoft .NET repository",
  "isActive": true,
  "clickCount": 0,
  "createdAt": "2024-01-15T10:30:00Z"
}
```

### 3. Test the Redirect
```bash
# This will redirect to the original URL
curl -L http://localhost:5135/abc12345
```

### 4. Check Analytics
```bash
# Get most popular URLs
curl http://localhost:5135/UrlShortener/MostClicked/10

# Get all active URLs  
curl http://localhost:5135/allActiveUrls
```

## Explore Your Environment

### Database Management
Visit **Supabase Studio** at http://localhost:8080
- Modern PostgreSQL management interface
- Browse tables, run queries, view schema
- No additional authentication required

### Cache Management
Visit **Redis Commander** at http://localhost:8081
- Username: `admin`
- Password: `admin`
- Inspect cache entries, monitor memory usage
- View hybrid cache structure (`redirect:*` and `entity:*` keys)

### API Documentation
Visit **Swagger UI** at http://localhost:5135/swagger
- Interactive API documentation
- Test endpoints directly from the browser
- View request/response schemas

### Monitoring
- **Health Checks**: http://localhost:5135/health
- **Metrics**: http://localhost:5135/metrics (Prometheus format)

## Alternative Setup (Local Databases)

If you prefer to run databases locally without Docker:

### Install Dependencies
```bash
# macOS (using Homebrew)
brew install postgresql@16 redis
brew services start postgresql redis

# Ubuntu/Debian
sudo apt update
sudo apt install postgresql-16 redis-server

# Windows - Use official installers
# PostgreSQL: https://www.postgresql.org/download/windows/
# Redis: https://github.com/microsoftarchive/redis/releases
```

### Configure Connection Strings
Edit `src/API/appsettings.Development.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=urlshortener;Username=postgres;Password=postgres"
  },
  "Redis": {
    "ConnectionString": "localhost:6379",
    "IsEnabled": true
  }
}
```

### Create Database
```bash
# Connect to PostgreSQL
psql -U postgres

# Create database
CREATE DATABASE urlshortener;
\q
```

### Run Migrations and Start API
```bash
dotnet build
dotnet ef database update --project src/Infrastructure --startup-project src/API
dotnet run --project src/API
```

## Development Workflow

### Hot Reload Development
```bash
# Run with automatic restart on code changes
dotnet watch --project src/API
```

### View Logs
```bash
# In another terminal, follow application logs
tail -f logs/urlshortener.log
```

### Run Tests
```bash
# Run all tests
dotnet test

# Run specific test project
dotnet test tests/UrlShortener.Api.Tests/
```

## Troubleshooting

### Services Won't Start
```bash
# Check Docker status
docker ps
docker-compose -f docker/docker-compose.dev.yml logs

# Restart specific service
docker-compose -f docker/docker-compose.dev.yml restart postgres
```

### Database Connection Issues
```bash
# Test PostgreSQL connectivity
docker exec -it postgres psql -U postgres -d urlshortener -c "SELECT 1;"

# Check migration status
dotnet ef migrations list --project src/Infrastructure --startup-project src/API
```

### Port Conflicts
```bash
# Check what's using a port (e.g., 5432)
lsof -i :5432

# Stop conflicting service
sudo lsof -t -i tcp:5432 | xargs kill -9
```

### Redis Connection Issues
```bash
# Test Redis connectivity
docker exec -it redis redis-cli ping
# Should return: PONG
```

## Next Steps

Now that you have the URL Shortener running:

1. **Explore the API** - Try different endpoints in Swagger UI
2. **Check the Database** - Use Supabase Studio to see your data  
3. **Monitor Cache** - Watch Redis Commander as you create URLs
4. **Review the Code** - Start with `src/API/Controllers/UrlShortenerController.cs`
5. **Learn the Architecture** - Read [Clean Architecture](./architecture/clean-architecture.md)
6. **Understand Caching** - Explore [Advanced Caching Patterns](./caching/patterns.md)

For detailed development setup and advanced configurations, see the [Local Development Setup](./development/local-setup.md) guide.
