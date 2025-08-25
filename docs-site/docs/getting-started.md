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

:::warning Command Correction
If you encountered an error like `no such service: ./docker/docker-compose.dev.yml`, you used the wrong command syntax. The correct format is:
- ✅ `docker-compose -f docker-compose.dev.yml up -d`
- ❌ `docker compose run ./docker/docker-compose.dev.yml -d` (incorrect)
:::

### 1. Clone and Navigate
```bash
git clone https://github.com/kufa-dev-team/url_shortener.git
cd url_shortener
```

### 2. Start Infrastructure Services
```bash
# Option 1: Use the root-level development compose file (recommended)
docker-compose -f docker-compose.dev.yml up -d

# Option 2: Use the simple docker-compose command (uses include from docker-compose.yml)  
docker-compose up -d

# Option 3: Use the compose file in docker directory
docker-compose -f docker/docker-compose.dev.yml up -d

# Verify services are running
docker-compose -f docker-compose.dev.yml ps
```

:::tip Command Syntax Note
The correct syntax is `docker-compose -f filename.yml up -d`, not `docker compose run`. The project has compose files in both the root directory and the `docker/` subdirectory.
:::

**Services Started:**
| Service | Port | URL | Purpose |
|---------|------|-----|---------|
| PostgreSQL | 5432 | - | Main database |
| Redis | 6379 | - | Caching layer |
| pgAdmin | 8082 | http://localhost:8082 | Database management UI |
| Redis Commander | 8081 | http://localhost:8081 | Redis management UI |
| Prometheus | 9090 | http://localhost:9090 | Metrics collection |
| Grafana | 3000 | http://localhost:3000 | Monitoring dashboards |
| cAdvisor | 8080 | http://localhost:8080 | Container monitoring |

### 3. Apply Database Migrations
```bash
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
- **HTTPS**: https://localhost:7127  
- **Swagger UI**: http://localhost:5135/swagger

:::info Docker vs Local Development
When running the API in Docker (production), it uses port 5000. These ports (5135/7127) are for local development only.
:::

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
Visit **pgAdmin** at http://localhost:8082
- **Email:** admin@admin.com
- **Password:** admin
- Modern PostgreSQL management interface
- Browse tables, run queries, view schema
- Connect to server: postgres, port 5432, database: urlshortener

### Cache Management
Visit **Redis Commander** at http://localhost:8081
- Username: `admin`
- Password: `admin`
- Inspect cache entries, monitor memory usage
- View hybrid cache structure (`redirect:*` and `entity:*` keys)

### Monitoring Stack
Visit **Grafana** at http://localhost:3000
- Username: `admin`
- Password: `admin` 
- Pre-configured dashboards for API, database, and Redis metrics

Visit **Prometheus** at http://localhost:9090
- Raw metrics collection and querying
- PromQL query interface
- Service discovery and health status

### API Documentation
Visit **Swagger UI** at http://localhost:5135/swagger
- Interactive API documentation
- Test endpoints directly from the browser
- View request/response schemas

### Monitoring
- **Health Checks**: http://localhost:5135/health
- **Metrics**: http://localhost:5135/metrics (Prometheus format)
- **Grafana Dashboards**: http://localhost:3000 (admin/admin)
- **Prometheus**: http://localhost:9090

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

# Check logs for specific issues
docker-compose -f docker-compose.dev.yml logs

# Restart specific service
docker-compose -f docker-compose.dev.yml restart postgres

# Stop and restart all services
docker-compose -f docker-compose.dev.yml down
docker-compose -f docker-compose.dev.yml up -d
```

### Database Connection Issues
```bash
# Test PostgreSQL connectivity (after containers are running)
docker exec -it urlshortener-postgres psql -U postgres -d urlshortener -c "SELECT 1;"

# Check migration status
dotnet ef migrations list --project src/Infrastructure --startup-project src/API
```

### Port Conflicts
```bash
# Check what's using a port (e.g., 5432)
lsof -i :5432

# Or use netstat
netstat -an | grep :5432

# Stop conflicting service
sudo lsof -t -i tcp:5432 | xargs kill -9
```

### Redis Connection Issues
```bash
# Test Redis connectivity
docker exec -it urlshortener-redis redis-cli ping
# Should return: PONG
```

### Common Docker Issues
```bash
# Remove obsolete version warning
# Edit docker-compose.yml and remove the "version: '3.8'" line

# Clean up Docker resources if needed
docker system prune -f
docker volume prune -f

# Rebuild containers if images are corrupted
docker-compose -f docker-compose.dev.yml down --volumes
docker-compose -f docker-compose.dev.yml up -d --build
```

## Next Steps

Now that you have the URL Shortener running:

1. **Explore the API** - Try different endpoints in Swagger UI
2. **Check the Database** - Use pgAdmin to see your data  
3. **Monitor Cache** - Watch Redis Commander as you create URLs
4. **Review the Code** - Start with `src/API/Controllers/UrlShortenerController.cs`
5. **Learn the Architecture** - Read [Clean Architecture](./architecture/clean-architecture.md)
6. **Understand Caching** - Explore [Advanced Caching Patterns](./caching/patterns.md)
7. **API Documentation** - Review [API Endpoints](./api/endpoints.md) and [DTOs](./api/dto.md)
8. **Monitoring Setup** - Check out [Monitoring & Observability](./monitoring/observability.md)

For more advanced topics, explore the other sections in this documentation.
