# Docker Guide for URL Shortener

## Overview

This directory contains all Docker-related files for the URL Shortener application.

### File Structure
```
docker/
├── Dockerfile                 # Production-optimized container image
├── docker-compose.yml        # Production environment setup
├── docker-compose.dev.yml    # Development environment setup
├── init-scripts/             # Database initialization scripts
│   └── 01-init.sql           # PostgreSQL schema setup
└── DOCKER.md                 # This documentation
```

## Quick Start

### Development Environment
```bash
# Start development environment (includes management UIs and monitoring)
docker-compose up -d  # Note: runs from project root, uses docker-compose.dev.yml

# Access services:
# - PgAdmin: http://localhost:8082 (admin@admin.com/admin)
# - Redis Commander: http://localhost:8081 (admin/admin)
# - Grafana: http://localhost:3000 (admin/admin)
# - Prometheus: http://localhost:9090
# - cAdvisor: http://localhost:8080
# - PostgreSQL: localhost:5432 (postgres/postgres)
# - Redis: localhost:6379 (no auth in dev)
```

### Production Environment
```bash
# Start production environment
docker-compose -f docker/docker-compose.yml up -d

# Access service:
# - API: http://localhost:5000
```

## Environment Comparison

| Feature | Development | Production |
|---------|-------------|------------|
| **API Service** | Not included (run locally) | Included with full config |
| **Container Names** | `urlshortener-*` | `urlshortener-*` |
| **Network Name** | `url_shortener_urlshortener-network` | `urlshortener_network` |
| **Database Ports** | Exposed (5432:5432) | Internal only (expose 5432) |
| **Redis Ports** | Exposed (6379:6379) | Internal only (expose 6379) |
| **Management UIs** | ✅ PgAdmin, Redis Commander, Grafana | ❌ Not included |
| **Monitoring Stack** | ✅ Full monitoring (Prometheus, Grafana, cAdvisor) | ❌ Not included |
| **Passwords** | Simple (`postgres`, no Redis auth) | Secure (from .env file) |
| **Platform** | `linux/arm64` (Apple Silicon optimized) | Multi-platform |
| **Security** | Basic | Production-hardened |

## Environment Variables

### Required .env File
Create a `.env` file in the project root (copy from `.env.example`):

```bash
# Database Configuration
POSTGRES_DB=urlshortener
POSTGRES_USER=postgres
POSTGRES_PASSWORD=SecurePassword123!

# Redis Configuration
REDIS_PASSWORD=SecureRedisPassword123!
REDIS_ENABLED=true
REDIS_DEFAULT_TTL_SECONDS=86400

# Application Configuration
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://*:8080
```

### Environment-Specific Variables

#### Development (.env for dev)
- Simple passwords (`postgres`, no Redis password)
- Debug logging enabled
- Management UIs included

#### Production (.env for prod)
- Strong passwords required
- Minimal logging
- Security-focused configuration
- No management interfaces

## Common Commands

### Building & Starting

```bash
# Build production image
docker-compose -f docker/docker-compose.yml build

# Start development (infrastructure only)
docker-compose -f docker/docker-compose.dev.yml up

# Start production (full application)
docker-compose -f docker/docker-compose.yml up -d

# View logs
docker-compose -f docker/docker-compose.yml logs -f api
```

### Database Operations

```bash
# Connect to PostgreSQL (development)
docker exec -it urlshortener-postgres psql -U postgres -d urlshortener

# Connect to PostgreSQL (production)
docker exec -it urlshortener-postgres psql -U postgres -d urlshortener

# Run database migrations (using migration script - recommended)
./scripts/migrate.sh        # Linux/macOS
.\scripts\migrate.ps1       # Windows PowerShell

# Alternative: Direct Docker command (if script is not available)
docker exec -it urlshortener-api dotnet ef database update
```

### Redis Operations

```bash
# Connect to Redis CLI (development - no auth)
docker exec -it urlshortener-redis redis-cli

# Connect to Redis CLI (production - with auth)
docker exec -it urlshortener-redis redis-cli -a SecureRedisPassword123!

# Monitor Redis commands
docker exec -it urlshortener-redis redis-cli monitor
```

### Cleanup & Maintenance

```bash
# Stop all services
docker-compose -f docker/docker-compose.yml down

# Stop and remove volumes (⚠️ destroys data)
docker-compose -f docker/docker-compose.yml down -v

# Remove unused images
docker system prune -f

# Remove everything related to this project
docker-compose -f docker/docker-compose.yml down -v --rmi all
```

## Network Architecture

### Development Environment
```
┌─────────────────┐    ┌──────────────────┐    ┌─────────────────┐
│  Your Machine   │    │   PostgreSQL     │    │     Redis       │
│  (API running   │────│  localhost:5432  │    │ localhost:6379  │
│   locally)      │    │                  │    │                 │
└─────────────────┘    └──────────────────┘    └─────────────────┘
                              │                         │
                    ┌─────────────────┐    ┌─────────────────┐
                    │ Supabase Studio │    │ Redis Commander │
                    │ localhost:8080  │    │ localhost:8081  │
                    └─────────────────┘    └─────────────────┘
```

### Production Environment
```
┌─────────────────┐    ┌──────────────────┐    ┌─────────────────┐
│      API        │    │   PostgreSQL     │    │     Redis       │
│ localhost:5000  │────│   (internal)     │    │   (internal)    │
│                 │    │    port 5432     │    │    port 6379    │
└─────────────────┘    └──────────────────┘    └─────────────────┘
          │
    urlshortener-network (internal Docker network)
```

## Configuration Details

### Dockerfile Features
- **Multi-stage build** for optimized image size (~250MB)
- **Non-root user** for security
- **Alpine Linux** base for minimal attack surface
- **Health checks** for container monitoring
- **Self-contained** .NET deployment

### Docker Compose Features
- **Health checks** for all services
- **Dependency management** (API waits for DB/Redis)
- **Named volumes** for data persistence
- **Restart policies** for reliability
- **Network isolation** for security

## Troubleshooting

### Common Issues

#### "Connection refused" errors
```bash
# Check if services are running
docker-compose -f docker/docker-compose.yml ps

# Check service logs
docker-compose -f docker/docker-compose.yml logs postgresql
docker-compose -f docker/docker-compose.yml logs redis
```

#### Port conflicts

**Common issue**: "Bind for 0.0.0.0:XXXX failed: port is already allocated"

```bash
# Check what processes are using common ports
lsof -i :5432  # PostgreSQL
lsof -i :6379  # Redis  
lsof -i :3000  # Grafana
lsof -i :8080  # cAdvisor
lsof -i :8081  # Redis Commander
lsof -i :9090  # Prometheus

# Find and stop conflicting Docker containers
docker ps | grep -E "(postgres|redis|grafana|prometheus)"
docker stop container_name_here

# Alternative: Stop entire conflicting project
# If you have another project using the same ports:
docker-compose -f /path/to/other/project/docker-compose.yml down

# Nuclear option: Stop all Docker containers
docker stop $(docker ps -q)
```

**Real-world example** (what we experienced):
```bash
# Problem: grad_tracking project was using the same ports
# Solution: Stop conflicting containers
docker stop grad_tracking_redis grad_tracking_postgres grad_tracking_app_dev
docker stop grad_tracking_keycloak grad_tracking_prometheus

# Then start your URL shortener services
docker-compose up -d
```

#### Permission denied
```bash
# Fix volume permissions
docker-compose -f docker/docker-compose.yml down -v
docker volume prune -f
docker-compose -f docker/docker-compose.yml up
```

#### Out of memory
```bash
# Check Docker resource limits
docker system df
docker system prune -f

# Monitor container memory usage
docker stats
```

### Database Connection Issues

#### Development Environment
- **Host**: `localhost`
- **Port**: `5432`
- **Database**: `urlshortener`
- **Username**: `postgres`
- **Password**: `postgres`

#### Production Environment
- **Host**: `postgres` (internal Docker network)
- **Port**: `5432`
- **Database**: `urlshortener`
- **Username**: `postgres`
- **Password**: From `.env` file

### Redis Connection Issues

#### Development Environment
- **Host**: `localhost`
- **Port**: `6379`
- **Password**: None

#### Production Environment
- **Host**: `redis` (internal Docker network)
- **Port**: `6379`
- **Password**: From `.env` file

## Database Migrations

The application uses Entity Framework Core for database management. After starting the PostgreSQL container, you need to apply database migrations.

### Migration Scripts (Recommended)

⚠️ **Critical**: The migration script requires the **production** Docker setup to be running.

```bash
# STEP 1: Ensure production containers are running
docker-compose -f docker/docker-compose.yml up -d

# STEP 2: Run migration script
./scripts/migrate.sh        # Linux/macOS
.\scripts\migrate.ps1       # Windows PowerShell
```

**What the script expects**:
- Container name: `urlshortener-postgres` (⚠️ **Not** `docker-postgres-1`)
- Network name: `urlshortener_network` (⚠️ **Not** `docker_default`)
- Environment file: `.env` in project root

These scripts will:
1. ✅ Check Docker prerequisites (correct container and network names)
2. 🏗️ Build migration Docker image using `docker/Dockerfile.migration`
3. 🚀 Apply EF Core migrations to the running PostgreSQL container
4. ✅ Verify completion

**Troubleshooting migrations**:
```bash
# Verify required containers are running
docker ps --filter "name=urlshortener-postgres"

# Verify network exists
docker network ls | grep urlshortener_network

# Check container logs if migration fails
docker logs urlshortener-postgres
```

### Manual Migration (Advanced)

If you prefer manual control:

```bash
# Build migration image
docker build -f Dockerfile.migration -t migration-runner .

# Run migration
docker run --rm --network urlshortener_network migration-runner \
  --connection "Host=postgres;Port=5432;Database=urlshortener;Username=postgres;Password=SecurePassword123!;"
```

### Troubleshooting Migrations

If migrations fail:
1. Ensure PostgreSQL container is healthy: `docker ps`
2. Check network exists: `docker network ls | grep urlshortener`
3. Verify .env file has correct credentials
4. Check migration logs for specific errors

## Best Practices

1. **Always use .env files** for sensitive configuration
2. **Never commit .env files** to version control
3. **Use named volumes** for data that should persist
4. **Monitor container health** with `docker-compose ps`
5. **Regular cleanup** with `docker system prune`
6. **Backup data volumes** before major changes
7. **Run migrations after starting PostgreSQL** but before using the API

## Integration with Development Workflow

### Local Development
1. Start infrastructure: `docker-compose -f docker/docker-compose.dev.yml up`
2. Run API locally with your IDE/debugger
3. Access management UIs for debugging

### Testing
1. Use production compose with test database
2. Run integration tests against Docker services
3. Validate with health checks

### Deployment
1. Build production image
2. Deploy with `docker-compose.yml`
3. Monitor with container health checks