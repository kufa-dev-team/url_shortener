# 🚀 Quick Setup Guide

This guide gets you up and running in under 5 minutes.

## Prerequisites
- Docker Desktop (running)
- .NET 9.0 SDK
- Git

## Step 1: Clone and Configure
```bash
git clone <repository-url>
cd url_shortener
cp .env.example .env  # Edit if needed
```

## Step 2: Choose Your Setup

### Option A: Development (Recommended for coding)
```bash
# Start infrastructure
docker-compose up -d

# Run API locally (in another terminal)
dotnet run --project src/API
```
✅ **API**: http://localhost:5135  
✅ **Database tools**: http://localhost:8082 (PgAdmin)  
✅ **Redis tools**: http://localhost:8081 (Redis Commander)  

### Option B: Production (For migrations)
```bash
# Start full stack
docker-compose -f docker/docker-compose.yml up -d

# Apply database migrations (REQUIRED)
./scripts/migrate.sh
```
✅ **API**: http://localhost:5000  
✅ **Database**: Accessible internally only  

## Step 3: Test the API
```bash
# Shorten a URL
curl -X POST http://localhost:5135/shorten \
  -H "Content-Type: application/json" \
  -d '{"originalUrl": "https://github.com/microsoft/dotnet"}'

# Expected response:
# {"shortUrl": "https://short.ly/abc123", "code": "abc123"}
```

## 🚨 Troubleshooting

### Port Already in Use?
```bash
# Find what's using the port
lsof -i :5432  # or :6379, :3000, :8080, :8081, :9090

# Stop conflicting containers
docker ps
docker stop <container_name>

# Try again
docker-compose up -d
```

### Migration Script Fails?
```bash
# Ensure production containers are running first
docker-compose -f docker/docker-compose.yml up -d

# Check containers are healthy
docker ps --filter "name=urlshortener-postgres"

# Then run migration
./scripts/migrate.sh
```

## 🎯 Next Steps
- Read the [full README](README.md) for detailed documentation
- Check [Docker guide](docker/Readme.md) for advanced setup
- Explore the [monitoring dashboard](http://localhost:3000) (Grafana)

## Common Commands
| Task | Command |
|------|---------|
| Stop all services | `docker-compose down` |
| View logs | `docker-compose logs -f` |
| Connect to database | `docker exec -it urlshortener-postgres psql -U postgres -d urlshortener` |
| Connect to Redis | `docker exec -it urlshortener-redis redis-cli` |
| Rebuild everything | `docker-compose down -v && docker-compose up -d --build` |

Happy coding! 🎉
