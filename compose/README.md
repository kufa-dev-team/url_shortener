# Docker Compose Development Environment

This folder contains Docker Compose configuration for running PostgreSQL and Redis locally for development.

## Services Included

- **PostgreSQL 16** - Primary database
- **Redis 7** - Caching layer
- **Supabase Studio** - Modern PostgreSQL management interface
- **Redis Commander** - Lightweight Redis management interface

## Quick Start

### Prerequisites
- **Docker Desktop** must be installed and running
- On Windows: Docker Desktop for Windows
- On macOS: Docker Desktop for Mac
- On Linux: Docker Engine + Docker Compose

### macOS/Linux
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

### Windows (PowerShell)
```powershell
# Navigate to compose directory
cd compose

# Copy environment file
Copy-Item .env.example .env

# Start all services
docker-compose -f docker-compose.dev.yml up -d

# Check service status
docker-compose -f docker-compose.dev.yml ps

# View logs
docker-compose -f docker-compose.dev.yml logs -f
```

### Windows (Command Prompt)
```cmd
# Navigate to compose directory
cd compose

# Copy environment file
copy .env.example .env

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
| Supabase Studio | http://localhost:8080 | Modern PostgreSQL UI |
| Redis Commander | http://localhost:8081 | admin/admin |

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

### All Platforms
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

## Troubleshooting

### Docker Daemon Not Running
**Error:** `Cannot connect to the Docker daemon`

**Solutions:**
- **Windows:** Start Docker Desktop from Start Menu
- **macOS:** Start Docker Desktop from Applications
- **Linux:** `sudo systemctl start docker`

### Port Already in Use
**Error:** `Port 5432 is already allocated`

**Solutions:**
```bash
# Check what's using the port
netstat -tulpn | grep :5432  # Linux/macOS
netstat -ano | findstr :5432  # Windows

# Stop local PostgreSQL/Redis services
sudo systemctl stop postgresql  # Linux
brew services stop postgresql   # macOS
# Windows: Stop via Services.msc

# Or change ports in docker-compose.dev.yml
```

### Permission Issues (Linux)
**Error:** Permission denied

**Solutions:**
```bash
# Add user to docker group
sudo usermod -aG docker $USER
# Logout and login again

# Or run with sudo (not recommended)
sudo docker-compose -f docker-compose.dev.yml up -d
```

### Windows Path Issues
**Error:** Invalid volume mount paths

**Solutions:**
- Ensure Docker Desktop has access to your drive (Settings > Resources > File Sharing)
- Use forward slashes in paths: `/c/Users/...` instead of `C:\Users\...`
- Enable WSL 2 backend in Docker Desktop settings

## Data Persistence

- PostgreSQL data: `postgres_data` volume
- Redis data: `redis_data` volume  
- No persistent UI data needed (Redis Commander is stateless)

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