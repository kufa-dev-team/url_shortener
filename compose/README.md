# Docker Compose Development Environment

This folder contains Docker Compose configuration for running PostgreSQL and Redis locally for development.

## Services Included

- **PostgreSQL 16** - Primary database
- **Redis 7** - Caching layer
- **pgAdmin 4** - PostgreSQL web interface
- **Redis Commander** - Redis web interface

## Quick Start

```bash
# Navigate to compose directory
cd compose

# Copy environment file
cp .env.example .env

# Start all services
docker-compose -f docker-compose.dev.yml up -d

# Check service status
docker-compose -f docker-compose.dev.yml ps

# View logs
docker-compose -f docker-compose.dev.yml logs -f
```

## Service Access

| Service | URL | Credentials |
|---------|-----|-------------|
| PostgreSQL | `localhost:5432` | postgres/postgres |
| Redis | `localhost:6379` | No auth |
| pgAdmin | http://localhost:8080 | admin@urlshortener.local/admin |
| Redis Commander | http://localhost:8081 | No auth |

## Connection Strings

### PostgreSQL
```
Host=localhost;Port=5432;Database=urlshortener;Username=postgres;Password=postgres
```

### Redis
```
localhost:6379
```

## Useful Commands

```bash
# Stop all services
docker-compose -f docker-compose.dev.yml down

# Stop and remove volumes (clean slate)
docker-compose -f docker-compose.dev.yml down -v

# Restart specific service
docker-compose -f docker-compose.dev.yml restart postgres

# Execute SQL in PostgreSQL
docker-compose -f docker-compose.dev.yml exec postgres psql -U postgres -d urlshortener

# Execute Redis commands
docker-compose -f docker-compose.dev.yml exec redis redis-cli

# View service logs
docker-compose -f docker-compose.dev.yml logs postgres
docker-compose -f docker-compose.dev.yml logs redis
```

## Data Persistence

- PostgreSQL data: `postgres_data` volume
- Redis data: `redis_data` volume  
- pgAdmin settings: `pgadmin_data` volume

Data persists between container restarts but can be removed with `docker-compose down -v`.

## Health Checks

Both PostgreSQL and Redis include health checks:
- PostgreSQL: `pg_isready` command
- Redis: `redis-cli ping` command

## Customization

Edit `docker-compose.dev.yml` to:
- Change port mappings
- Modify resource limits
- Add additional services
- Configure different versions