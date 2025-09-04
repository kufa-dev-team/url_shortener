<div align="center">
  <img src="docs-site/static/img/repo_logo.webp" alt="URL Shortener Logo" width="200" height="200">
  
  # URL Shortener
  
  **A high-performance URL shortener service built with ASP.NET Core 9.0**
  
  *Featuring advanced caching strategies and modern C# development practices*
  
  [![.NET](https://img.shields.io/badge/.NET-9.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
  [![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16-316192?style=for-the-badge&logo=postgresql&logoColor=white)](https://www.postgresql.org/)
  [![Redis](https://img.shields.io/badge/Redis-7-DC382D?style=for-the-badge&logo=redis&logoColor=white)](https://redis.io/)
  [![Docker](https://img.shields.io/badge/Docker-Ready-2496ED?style=for-the-badge&logo=docker&logoColor=white)](https://www.docker.com/)
</div>

## 🚀 Quick Reference

| Scenario | Commands | Notes |
|----------|----------|-------|
| **Development** | `docker-compose up -d`<br>`dotnet run --project src/API` | For daily coding, debugging |
| **Production/Migration** | `docker-compose -f docker/docker-compose.yml up -d`<br>`./scripts/migrate.sh` | Required for database migrations |
| **Fix Port Conflicts** | `docker ps`<br>`docker stop conflicting_container` | Check troubleshooting section |
| **Reset Everything** | `docker-compose down -v` | ⚠️ Destroys all data |

## 🎯 Overview

Transform long URLs into short, branded links:

**Transform this:**
```
https://www.example.com/very/long/url/with/many/parameters?param1=value1&param2=value2
```

**Into this:**
```
https://short.ly/abc123
```

## 🏗️ Architecture

This project follows Clean Architecture principles with clear separation of concerns:

```
src/
├── API/                    # Web API layer (Controllers, Program.cs)
├── Application/           # Business logic and services
├── Domain/               # Core entities and interfaces
└── Infrastructure/       # Data access and external services
```

## 🎗️ Getting Started

🚀 **New here?** Check out our [Quick Setup Guide](SETUP.md) to get running in under 5 minutes!

### Prerequisites

- .NET 9.0 SDK
- **Docker Desktop** (Windows/Mac) or Docker Engine (Linux)
- Your favorite IDE (Visual Studio, VS Code, Rider)

**Important:** Make sure Docker Desktop is running before starting the services!

### Quick Start with Docker

⚠️ **Important**: Check for port conflicts before starting! This project uses common ports (5432, 6379, 3000, 8080, 8081, 9090) that might already be in use.

1. **Setup environment configuration:**
```bash
# Copy and edit environment configuration
cp .env.example .env  # Edit passwords and settings as needed
```

2. **Choose your setup:**

#### Option A: Development Environment (Recommended for local development)
```bash
# Start development infrastructure (databases + monitoring tools)
docker-compose up -d

# Run API locally for debugging
dotnet run --project src/API
```

#### Option B: Production Environment (Full containerized stack)
```bash
# Start full application stack in containers
docker-compose -f docker/docker-compose.yml up -d
```

3. **Apply database migrations:**
```bash
# For production setup (required)
./scripts/migrate.sh

# For development setup (if using containerized API)
# The migration script automatically detects the correct setup
```

4. **Access the application:**
- **Development API**: http://localhost:5135 (when running locally)
- **Production API**: http://localhost:5000 (containerized)
- **Database**: Available at `localhost:5432`
- **Redis**: Available at `localhost:6379`

### Development Services

Once Docker Compose is running, you'll have access to:

| Service | URL | Purpose |
|---------|-----|---------|
| PostgreSQL | `localhost:5432` | Main database |
| Redis | `localhost:6379` | Caching layer |
| PgAdmin | http://localhost:8082 | PostgreSQL management UI |
| Redis Commander | http://localhost:8081 | Redis management UI |
| Grafana | http://localhost:3000 | Monitoring dashboards |
| Prometheus | http://localhost:9090 | Metrics collection |
| cAdvisor | http://localhost:8080 | Container monitoring |

**Default credentials:**
- PostgreSQL: `postgres/postgres`
- PgAdmin: `admin@admin.com/admin`
- Redis Commander: `admin/admin`
- Grafana: `admin/admin`

⚠️ **Port Conflicts**: If you encounter "port already allocated" errors, you may need to stop other Docker containers or services using these ports.

## 📊 Development vs Production Setup Guide

### Development Setup (`docker-compose.dev.yml`)

**When to use**: Local development, debugging, testing

**What it includes**:
- PostgreSQL (exposed on port 5432)
- Redis (exposed on port 6379)  
- PgAdmin (PostgreSQL management UI)
- Redis Commander (Redis management UI)
- Grafana (monitoring dashboards)
- Prometheus (metrics collection)
- cAdvisor (container monitoring)
- Various exporters for monitoring

**How to use**:
```bash
# Start development infrastructure
docker-compose up -d

# Run your API locally for debugging
dotnet run --project src/API
# API will be available at http://localhost:5135
```

**Benefits**:
- ✅ Full debugging capabilities in your IDE
- ✅ Hot reload and fast iteration
- ✅ Access to management UIs
- ✅ All ports exposed for easy access

### Production Setup (`docker/docker-compose.yml`)

**When to use**: Production deployments, migration testing, full containerization

**What it includes**:
- Complete ASP.NET Core API (containerized)
- PostgreSQL (internal Docker network only)
- Redis (internal Docker network only)
- Optimized for security and performance

**How to use**:
```bash
# Start full containerized stack
docker-compose -f docker/docker-compose.yml up -d

# Run migrations (required for database setup)
./scripts/migrate.sh

# API available at http://localhost:5000
```

**Benefits**:
- ✅ Production-like environment
- ✅ Security-hardened (no unnecessary port exposure)
- ✅ Container-based deployment testing
- ✅ Required for running migrations

### When to Switch Between Setups

| Scenario | Recommended Setup | Command |
|----------|-------------------|----------|
| Daily development | Development | `docker-compose up -d` |
| Database migrations | Production | `docker-compose -f docker/docker-compose.yml up -d` |
| Testing containerized app | Production | `docker-compose -f docker/docker-compose.yml up -d` |
| Debugging API issues | Development | `docker-compose up -d` + local API |
| Production deployment | Production | `docker-compose -f docker/docker-compose.yml up -d` |

### Local Development (Alternative)

If you prefer to run databases locally without Docker:

```bash
# Install PostgreSQL and Redis locally
# Update connection strings in appsettings.json

# Build and run
dotnet build
dotnet run --project src/API
```

### API Usage

```bash
# Shorten a URL
curl -X POST http://localhost:5135/shorten \
  -H "Content-Type: application/json" \
  -d '{"originalUrl": "https://www.example.com/very/long/url"}'

# Response: {"shortUrl": "https://short.ly/abc123", "code": "abc123"}

# Redirect using short code
curl -L http://localhost:5135/abc123
# Redirects to original URL
```

### Environment Configuration

The application uses the following connection strings (configured in `appsettings.json`):

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=urlshortener;Username=postgres;Password=postgres",
    "Redis": "localhost:6379"
  }
}
```

## 🔧 Technology Stack

- **Framework**: ASP.NET Core 9.0
- **Database**: PostgreSQL 16 with Entity Framework Core
- **Caching**: Redis 7 with StackExchange.Redis
- **Architecture**: Clean Architecture
- **Patterns**: Repository, Dependency Injection
- **Development**: Docker Compose for local services
- **Admin Tools**: Supabase Studio, Redis Commander

## 📚 Features

### Core Functionality
- **URL Shortening** - Convert long URLs to short codes
- **URL Redirection** - Fast redirect from short codes to original URLs
- **Collision Handling** - Secure random code generation
- **Performance Optimized** - Advanced caching patterns

### Advanced C# Features Demonstrated
- **Generic Constraints** - Type safety with `where T : BaseEntity`
- **Nullable Reference Types** - Modern null handling
- **Pattern Matching** - Switch expressions and when clauses
- **Records** - Immutable data structures
- **Async/Await** - Asynchronous programming patterns
- **LINQ** - Complex query operations
- **Dependency Injection** - Loose coupling and testability

### Caching Patterns

The codebase includes comprehensive examples of Redis caching patterns:

#### Cache-Aside (Lazy Loading)
```
Application → Cache (miss) → Database → Cache (store) → Application
```
Most common pattern, application controls caching logic.

#### Read-Through
```
Application → Smart Cache → Database (on miss)
```
Cache automatically loads data on cache misses.

#### Write-Through
```
Application → Cache → Database (synchronous)
```
Strong consistency, data written to both cache and database.

#### Write-Behind (Write-Back)
```
Application → Cache (immediate) → Database (async)
```
High performance writes, eventual consistency.

## 📖 Code Examples

### Repository Pattern Usage
```csharp
// Simple CRUD operations
var entity = await repository.GetByIdAsync(1);
var allEntities = await repository.GetAllAsync();
await repository.AddAsync(newEntity);
await repository.UpdateAsync(entity);
await repository.DeleteAsync(1);
```

### Advanced LINQ Queries
```csharp
// Complex filtering with performance optimization
var results = await _dbSet
    .Where(x => x.CreatedAt > DateTime.UtcNow.AddDays(-30) && x.IsActive)
    .OrderByDescending(x => x.UpdatedAt)
    .AsNoTracking()
    .ToListAsync();
```

### Redis Caching Implementation
```csharp
// Cache-aside pattern example
var cacheKey = $"url_{shortCode}";
var cachedUrl = await _distributedCache.GetStringAsync(cacheKey);
if (cachedUrl != null)
{
    return JsonSerializer.Deserialize<ShortenedUrlResponse>(cachedUrl);
}

var shortenedUrl = await _dbSet.FirstOrDefaultAsync(x => x.ShortCode == shortCode);
if (shortenedUrl != null)
{
    var serialized = JsonSerializer.Serialize(shortenedUrl);
    await _distributedCache.SetStringAsync(cacheKey, serialized, 
        new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
        });
}
return shortenedUrl;
```

## Redis Hybrid Cache System & Invalidation

### Cache Architecture
**Dual-Tier Hybrid Caching Strategy:**

1. **Redirect Cache** (High-frequency operations)
   - Key: `redirect:{shortCode}`
   - TTL: 6 hours
   - Data: Lightweight RedirectCache model (~50-100 bytes)
   - Purpose: Optimized for URL redirection performance

2. **Entity Cache** (Low-frequency operations)
   - Keys: `entity:id:{id}`, `entity:short:{shortCode}`
   - TTL: 1 hour
   - Data: Full UrlMapping entities (~200-500 bytes)
   - Purpose: Complete data for CRUD operations

### Cache Invalidation

#### Automatic Expiry Policy
- **Redirect Cache**: 6-hour TTL for frequent redirects
- **Entity Cache**: 1-hour TTL for detailed operations
- **Bulk Deactivation**: Expired URLs deactivated via optimized bulk operations

#### Manual Admin Purge Endpoint

**Endpoint:** `DELETE /admin/cache/{shortCode}`

**Enhanced Implementation:**
```csharp
public async Task<bool> RemoveAsync(string shortCode)
{
    if (_redis == null) return false;
    
    // Enhanced cache invalidation for hybrid system
    var deleteTasks = new[]
    {
        _redis.KeyDeleteAsync($"redirect:{shortCode}"),       // Redirect cache
        _redis.KeyDeleteAsync($"entity:short:{shortCode}"),   // Entity cache  
        _redis.KeyDeleteAsync($"url:short:{shortCode}")       // Legacy compatibility
    };
    
    var results = await Task.WhenAll(deleteTasks);
    return results.Any(r => r);
}
```

**API Controller:**
```csharp
[HttpDelete("admin/cache/{shortCode}")]
public async Task<IActionResult> PurgeByCode(string shortCode)
{
    var deleted = await _urlMappingService.RemoveAsync(shortCode);
    if (!deleted)
        return NotFound($"ShortCode '{shortCode}' not found in cache.");
    return NoContent(); // 204
}
```

**Responses:**
- `204 No Content`: Cache successfully purged
- `404 Not Found`: ShortCode not found in any cache tier

### Performance Benefits
- **60-80% memory reduction** for redirect operations
- **Faster redirects** with lightweight cache payload
- **Better cache hit rates** with optimized TTL strategies
- **Complete cache invalidation** across all tiers

## 🎯 Key Learning Areas

### Performance Optimization
- **AsNoTracking()** for read-only queries
- **Bulk operations** with ExecuteDeleteAsync
- **Connection pooling** strategies
- **Async/await** best practices

### Modern C# Patterns
- **Null-coalescing assignment** (`??=`)
- **Switch expressions** for pattern matching
- **Init-only properties** for immutability
- **Record types** for data transfer
- **Local functions** for validation

### Scalability Considerations
- **Distributed caching** strategies
- **Database optimization** techniques
- **Memory management** with Span<T>
- **High-performance serialization**

## 🔍 Educational Resources

The `src/Infrastructure/Repositories/Repository.cs` file contains extensive educational comments including:

- **200+ lines of learning material**
- **ASCII diagrams** explaining caching patterns
- **Real-world Redis examples**
- **Performance optimization tips**
- **Advanced C# feature demonstrations**

## 🏗️ Project Structure

```
url_shortener/
├── src/
│   ├── API/
│   │   ├── Controllers/         # API endpoints
│   │   └── Program.cs          # Application startup
│   ├── Application/
│   │   └── Services/           # Business logic
│   ├── Domain/
│   │   ├── Entities/           # Core entities
│   │   └── Interfaces/         # Contracts
│   └── Infrastructure/
│       ├── Data/               # Database context
│       └── Repositories/       # Data access
├── compose/
│   ├── docker-compose.dev.yml  # Development services
│   ├── init-scripts/           # Database initialization
│   ├── .env.example           # Environment template
│   └── README.md              # Compose documentation
└── README.md
```

## 🐳 Docker Development Environment

The `compose/` folder contains everything needed for local development:

- **PostgreSQL 16** with automatic initialization
- **Redis 7** with persistence and memory optimization
- **Supabase Studio** for modern database management
- **Redis Commander** for cache inspection
- **Health checks** for all services
- **Data persistence** across container restarts

See `compose/README.md` for detailed Docker Compose usage instructions.

## 📊 Database Migrations

The application uses Entity Framework Core for database management. After starting PostgreSQL, apply migrations using the provided scripts:

### Migration Scripts (Recommended)

⚠️ **Important**: The migration script requires the **production** Docker setup to be running:

```bash
# REQUIRED: Start production containers first
docker-compose -f docker/docker-compose.yml up -d

# THEN run migration
./scripts/migrate.sh        # Linux/macOS
.\scripts\migrate.ps1       # Windows PowerShell
```

The migration scripts will:
1. ✅ Verify Docker prerequisites (container `urlshortener-postgres` running, network `urlshortener_network` exists)
2. 🏗️ Build temporary migration Docker image with EF Core tools
3. 🚀 Apply all pending database migrations
4. ✅ Confirm successful completion

**Note**: The migration script looks for specific container names (`urlshortener-postgres`) and network names (`urlshortener_network`) that are created by the production Docker Compose setup.

### Manual Migration (Advanced)
```bash
# Build migration container
docker build -f Dockerfile.migration -t migration-runner .

# Run migrations
docker run --rm --network urlshortener_network migration-runner \
  --connection "Host=postgres;Port=5432;Database=urlshortener;Username=postgres;Password=YourPassword;"
```

### Troubleshooting Migrations
If migrations fail:
- Ensure PostgreSQL container is healthy: `docker ps`
- Verify Docker network exists: `docker network ls | grep urlshortener`
- Check .env file credentials
- Review migration logs for specific errors

## 🔧 Troubleshooting

### Docker Port Conflicts

If you encounter "port already allocated" errors, follow these steps:

#### 1. Identify Conflicting Services
```bash
# Check what's using specific ports
lsof -i :5432  # PostgreSQL
lsof -i :6379  # Redis
lsof -i :3000  # Grafana
lsof -i :8080  # cAdvisor
lsof -i :8081  # Redis Commander
lsof -i :9090  # Prometheus
```

#### 2. Stop Conflicting Docker Containers
```bash
# List all running containers
docker ps

# Stop specific conflicting containers
docker stop container_name_here

# Or stop all containers from another project
docker-compose -f /path/to/other/project/docker-compose.yml down
```

#### 3. Common Port Conflicts
- **Port 5432**: Usually PostgreSQL from another project
- **Port 6379**: Usually Redis from another project  
- **Port 3000**: Usually Grafana or React dev server
- **Port 8080**: Usually Keycloak or other web applications
- **Port 9090**: Usually Prometheus from another monitoring stack

#### 4. Alternative: Change Ports
If you can't stop the conflicting services, modify the port mappings in `docker-compose.dev.yml`:
```yaml
postgres:
  ports:
    - "15432:5432"  # Changed from 5432:5432
```

### Migration Issues

#### Container Name Mismatches
If the migration script reports container not found:
```bash
# Check actual container names
docker ps --filter "name=postgres"

# The migration script expects 'urlshortener-postgres'
# If your container has a different name, the script needs updating
```

#### Network Issues
```bash
# Check if the Docker network exists
docker network ls | grep urlshortener

# If missing, restart Docker Compose
docker-compose down
docker-compose up -d
```

## 🚀 Future Enhancements

- **Authentication** - Add JWT-based security
- **Rate Limiting** - Prevent abuse  
- **Analytics** - Track click statistics
- **Custom Domains** - Support branded short URLs
- **Bulk Operations** - Process multiple URLs
- **API Versioning** - Support multiple API versions

## 🤝 Contributing

Contributions are welcome! Please feel free to submit pull requests or open issues for:
- Bug fixes
- Performance improvements
- Additional caching patterns
- Documentation enhancements

## 📚 Additional Resources

- [Clean Architecture](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
- [Redis Caching Patterns](https://redis.io/docs/manual/patterns/)
- [ASP.NET Core Performance](https://docs.microsoft.com/en-us/aspnet/core/performance/)
- [Entity Framework Performance](https://docs.microsoft.com/en-us/ef/core/performance/)

---

This URL shortener demonstrates modern C# development practices, advanced caching strategies, and clean architecture principles. Explore the codebase to learn about high-performance web API development with .NET.





# Staging Deployment Guide

This document provides a clear guide for deploying the URL Shortener service to a local staging environment using Docker Compose. 

## Overview
The staging environment runs inside Docker containers and includes:

- API: ASP.NET Core 9.0 application
- PostgreSQL (database)
- Redis (caching layer)

## Prerequisites
- Docker Engine
- Docker Compose
- .NET 9.0 SDK (optional, for running migrations locally)

## Environment Variables & Secrets Manifest
Secrets for the staging environment are managed via a .env.staging file, which must not be committed to version control.

# Create the Environment File
- Create a new file named .env.staging in the docker/ directory.
- Copy the template below and paste it into the file.

```
ASPNETCORE_ENVIRONMENT=Staging

# ----- PostgreSQL Database -----
POSTGRES_DB=urlshortener_staging
POSTGRES_USER=appuser
POSTGRES_PASSWORD=your_secure_password_123 # Change this to a strong password

# ----- Redis -----
REDIS_HOST=redis
REDIS_PORT=6379

# -- pgAdmin --
PGADMIN_DEFAULT_EMAIL=admin@admin.com
PGADMIN_DEFAULT_PASSWORD=admin # Change this to a strong password
```

## Configuration Files
The application reads these values into different configuration layers:

# -Docker Services (Postgres, pgAdmin): 
Read POSTGRES_* and PGADMIN_* variables directly from the .env.staging file.

# -ASP.NET Core App: 
The connection strings are injected via appsettings.Staging.json which uses placeholders that are overridden by the Docker Compose file, which in turn reads from .env.staging.

- ConnectionStrings__DefaultConnection: Host=postgres;Port=5432;Database=${POSTGRES_DB};Username=${POSTGRES_USER};Password=${POSTGRES_PASSWORD}

- ConnectionStrings__Redis: redis:6379


## Deployment Guide (Staging)

Follow these steps to deploy the application to staging:

# Navigate to the compose directory
  cd docker

# Start the staging services

- Linux/macOS => docker compose -f docker-compose.staging.yml up -d --build
- Windows (PowerShell) => docker-compose -f docker-compose.staging.yml up -d --build

The -d flag runs containers in the background (detached mode). The --build flag ensures the API image is rebuilt with any recent code changes.

# Verify services are running
windows=>  docker-compose -f docker-compose.staging.yml ps
linux =>   docker compose -f docker/docker-compose.staging.yml ps

# View logs
windows=>  docker-compose -f docker-compose.staging.yml logs -f
linux =>   docker compose -f docker/docker-compose.staging.yml logs -f
# Run database migrations (if required)
  run ./scripts/migrate.sh

# Access the API
API → http://localhost:5135
PostgreSQL → localhost:5432 (inside Docker network: postgres:5432)
Redis → localhost:6379 (inside Docker network: redis:6379)

## Resetting the Staging Environment
If things break badly (wrong DB schema, bad volumes, etc.):

Linux / macOS:
docker compose -f docker/docker-compose.staging.yml down -v

Windows (PowerShell):
docker-compose -f docker-compose.staging.yml down -v

This removes all containers and volumes so you start fre

## Debugging 
# -password authentication failed for user "postgres"

- Error:
`28P01: password authentication failed for user "postgres"`

- Cause:
The Postgres container may still have old credentials stored in its Docker volume.
Solution:
Remove the Postgres volume so it can be recreated with the correct password.

- Linux / macOS
docker volume rm compose_postgres_data
docker volume rm docker_postgres_data
docker volume rm urlshortener_postgres_data


- Windows (PowerShell)
docker volume rm compose_postgres_data
docker volume rm docker_postgres_data
docker volume rm urlshortener_postgres_data


- After removing, restart your services:
docker compose -f docker/docker-compose.staging.yml up -d --build

# -Orphan container warnings

- Error:
`found orphan containers ([urlshortener-redis-ui]) for this project`


- Cause:
Containers from old Compose projects are still running.

- Solution:
Remove old orphan containers.

- Linux / macOS
docker ps -a
docker rm -f <container_id>


- Windows (PowerShell)
docker ps -a
docker rm -f <container_id>


Or let Docker handle it automatically:
docker compose -f docker/docker-compose.staging.yml up -d --remove-orphans


# -Database not updating after schema changes (migrations not applied)

- Cause:
EF Core migrations have not been applied to the Postgres database.

- Solution:
Run migrations inside the API container.

- Linux / macOS
docker exec -it <api_container_name> dotnet ef database update


- Windows (PowerShell)
docker exec -it <api_container_name> dotnet ef database update

Replace <api_container_name> with the name shown in docker ps (e.g., docker-api-1).


# -Wrong volume names

- Problem:
Docker Compose automatically prefixes volume names with the project name if not explicitly set.
For example, postgres_data: becomes compose_postgres_data.

- Solution:
Define explicit names in your docker-compose.staging.yml:

volumes:
  postgres_data:
    name: urlshortener_postgres_data
  redis_data:
    name: urlshortener_redis_data
  pgadmin_data:
    name: urlshortener_pgadmin_data


- Then remove old ones before recreating:

Linux / macOS
docker volume rm compose_postgres_data
docker volume rm compose_redis_data


Windows (PowerShell)
docker volume rm compose_postgres_data
docker volume rm compose_redis_data


⚡ Tip: Always check volumes and containers to see what is running and what may cause conflicts.

- List volumes:
docker volume ls

- List containers:
docker ps -a


