# URL Shortener

A high-performance URL shortener service built with ASP.NET Core 9.0, featuring advanced caching strategies and modern C# development practices.

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

## 🚀 Getting Started

### Prerequisites

- .NET 9.0 SDK
- **Docker Desktop** (Windows/Mac) or Docker Engine (Linux)
- Your favorite IDE (Visual Studio, VS Code, Rider)

**Important:** Make sure Docker Desktop is running before starting the services!

### Quick Start with Docker

1. **Start the infrastructure services:**

**macOS/Linux:**
```bash
# Navigate to compose directory
cd compose

# Copy environment configuration
cp .env.example .env

# Start PostgreSQL and Redis
docker-compose -f docker-compose.dev.yml up -d

# Verify services are running
docker-compose -f docker-compose.dev.yml ps
```

**Windows (PowerShell):**
```powershell
# Navigate to compose directory
cd compose

# Copy environment configuration
Copy-Item .env.example .env

# Start PostgreSQL and Redis
docker-compose -f docker-compose.dev.yml up -d

# Verify services are running
docker-compose -f docker-compose.dev.yml ps
```

2. **Run the application:**
```bash
# Return to project root
cd ..

# Build the solution
dotnet build

# Run the API
dotnet run --project src/API
```

The application will start on `http://localhost:5135`

### Development Services

Once Docker Compose is running, you'll have access to:

| Service | URL | Purpose |
|---------|-----|---------|
| PostgreSQL | `localhost:5432` | Main database |
| Redis | `localhost:6379` | Caching layer |
| Supabase Studio | http://localhost:8080 | Modern PostgreSQL management UI |
| Redis Commander | http://localhost:8081 | Redis management UI |

**Default credentials:**
- PostgreSQL: `postgres/postgres`
- Redis Commander: `admin/admin`
- Supabase Studio: Direct database connection (no separate auth)

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
    return JsonSerializer.Deserialize<UrlMapping>(cachedUrl);
}

var urlMapping = await _dbSet.FirstOrDefaultAsync(x => x.ShortCode == shortCode);
if (urlMapping != null)
{
    var serialized = JsonSerializer.Serialize(urlMapping);
    await _distributedCache.SetStringAsync(cacheKey, serialized, 
        new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
        });
}
return urlMapping;
```

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

## 🚀 Future Enhancements

- **Redis Integration** - Implement distributed caching
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