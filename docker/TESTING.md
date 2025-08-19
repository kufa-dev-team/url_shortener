# Docker Testing Guide

This guide covers testing procedures for the URL Shortener Docker deployment to ensure everything works correctly on any development machine.

## Quick Testing

### Automated Testing Scripts

Run one of these scripts to perform comprehensive automated testing:

```bash
# Linux/Mac
./test-docker.sh

# Windows (PowerShell)
.\test-docker.ps1

# Windows (with environment selection)
.\test-docker.ps1 -Environment production
.\test-docker.ps1 -Environment development
```

**What the scripts test**:
- ✅ Docker availability
- ✅ Service build and startup
- ✅ Health check validation
- ✅ API endpoint connectivity
- ✅ Image size requirements (<250MB)
- ✅ Database and Redis connectivity

## Manual Testing

### Step 1: Clean Environment
```bash
# Clean up any existing containers and volumes
docker-compose --env-file .env -f docker/docker-compose.yml down -v
docker system prune -f
```

### Step 2: Start Services
```bash
# Production environment
docker-compose --env-file .env -f docker/docker-compose.yml up -d

# Development environment (infrastructure only)
docker-compose -f docker/docker-compose.dev.yml up -d
```

### Step 3: Verify Service Status
```bash
# Check container status
docker-compose --env-file .env -f docker/docker-compose.yml ps

# Expected output:
# NAME                    STATUS
# urlshortener-api        Up (healthy)
# urlshortener-postgres   Up (healthy)  
# urlshortener-redis      Up (healthy)
```

### Step 4: Test Health Endpoints

```bash
# Test API liveness (basic API availability)
curl -f http://localhost:5000/health/live

# Test readiness (database + Redis connectivity)
curl -f http://localhost:5000/health/ready

# Test comprehensive health check
curl -f http://localhost:5000/health

# All should return HTTP 200 with {"status":"Healthy"}
```

### Step 5: Validate Requirements

```bash
# Check image size (must be < 250MB)
docker images docker-api --format "table {{.Repository}}\t{{.Tag}}\t{{.Size}}"

# Check service logs (should show no errors)
docker-compose --env-file .env -f docker/docker-compose.yml logs api
```

## Expected Test Results

### Successful Health Check Responses

**Liveness Check** (`/health/live`):
```json
{
  "status": "Healthy",
  "totalDuration": "00:00:00.0001010",
  "entries": {
    "api-self": {
      "status": "Healthy",
      "description": "API is responding",
      "tags": ["api"]
    }
  }
}
```

**Readiness Check** (`/health/ready`):
```json
{
  "status": "Healthy",
  "totalDuration": "00:00:00.1408225",
  "entries": {
    "database": {
      "status": "Healthy",
      "tags": ["db", "postgresql"]
    },
    "redis-cache": {
      "status": "Healthy", 
      "tags": ["cache", "redis"]
    }
  }
}
```

**Comprehensive Check** (`/health`):
```json
{
  "status": "Healthy",
  "entries": {
    "database": {"status": "Healthy"},
    "redis-cache": {"status": "Healthy"},
    "api-self": {"status": "Healthy"}
  }
}
```

### Performance Benchmarks

| Metric | Expected Value | Validation |
|--------|----------------|------------|
| **Docker Image Size** | < 250MB | `docker images docker-api` |
| **Startup Time** | < 2 minutes | Time from `up` to healthy |
| **Health Check Response** | < 500ms | `curl` response time |
| **Memory Usage** | < 512MB total | `docker stats` |

## Troubleshooting

### Environment Variable Warnings

**Problem**:
```
level=warning msg="The \"POSTGRES_DB\" variable is not set. Defaulting to a blank string."
```

**Solution**:
```bash
# Always use --env-file flag when running compose from docker/ directory
docker-compose --env-file .env -f docker/docker-compose.yml up
```

**Root Cause**: Docker Compose looks for `.env` in the same directory as the compose file.

### Database Migration Conflicts

**Problem**:
```
Npgsql.PostgresException: 42P07: relation "UrlMappings" already exists
```

**Solution**:
```bash
# Remove all volumes to clean database state
docker-compose --env-file .env -f docker/docker-compose.yml down -v
docker volume prune -f
docker-compose --env-file .env -f docker/docker-compose.yml up -d
```

### Port Conflicts

**Problem**:
```
Error: bind: address already in use
```

**Solution**:
```bash
# Check what's using the ports
netstat -tulpn | grep -E "(5000|5432|6379)"

# Stop conflicting services
sudo systemctl stop postgresql  # If local PostgreSQL is running
sudo systemctl stop redis       # If local Redis is running

# Or change ports in docker-compose.yml
```

### Container Restart Loops

**Problem**: API container keeps restarting

**Diagnosis**:
```bash
# Check logs for errors
docker-compose --env-file .env -f docker/docker-compose.yml logs api

# Common issues:
# - Database connection failures
# - Missing environment variables
# - Application configuration errors
```

**Solution**:
```bash
# Restart with fresh database
docker-compose --env-file .env -f docker/docker-compose.yml down -v
docker-compose --env-file .env -f docker/docker-compose.yml up -d

# Wait for database to be fully ready
docker-compose --env-file .env -f docker/docker-compose.yml logs postgres | grep "ready to accept connections"
```

### Slow Startup Times

**Problem**: Services take longer than 2 minutes to become healthy

**Diagnosis**:
```bash
# Monitor startup progress
docker-compose --env-file .env -f docker/docker-compose.yml logs -f

# Check resource usage
docker stats
```

**Solutions**:
- Increase Docker Desktop memory allocation (minimum 4GB recommended)
- Close other resource-intensive applications
- Use SSD storage for Docker volumes

## CI/CD Integration

### GitHub Actions Example

```yaml
name: Docker Tests
on: [push, pull_request]

jobs:
  docker-test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - name: Run Docker Tests
        run: |
          chmod +x test-docker.sh
          ./test-docker.sh
```

### Environment-Specific Testing

```bash
# Test with different environment configurations
cp .env.example .env.test

# Modify .env.test for testing
sed -i 's/POSTGRES_DB=urlshortener/POSTGRES_DB=urlshortener_test/' .env.test

# Run tests with test environment
docker-compose --env-file .env.test -f docker/docker-compose.yml up -d
```

## Test Maintenance

### Regular Testing Schedule

- **Before deployment**: Always run full test suite
- **Weekly**: Automated health checks
- **After changes**: Any Docker, database, or environment changes

### Updating Test Scripts

When modifying test scripts, ensure:
1. All docker-compose commands use `--env-file .env`
2. Proper cleanup in failure scenarios
3. Clear error messages and status reporting
4. Cross-platform compatibility (Windows/Linux/Mac)

### Test Data Management

```bash
# Reset to clean state
docker-compose --env-file .env -f docker/docker-compose.yml down -v

# Backup test data (if needed)
docker exec urlshortener-postgres pg_dump -U postgres urlshortener > backup.sql

# Restore test data
docker exec -i urlshortener-postgres psql -U postgres urlshortener < backup.sql
```

## Success Criteria

Your Docker setup is working correctly when:

- ✅ All automated tests pass
- ✅ All health endpoints return HTTP 200 with `"status": "Healthy"`
- ✅ Docker image size is under 250MB
- ✅ Services start within 2 minutes
- ✅ No error messages in container logs
- ✅ Database migrations apply successfully
- ✅ Redis cache is accessible and functional

## Getting Help

If tests continue to fail after following this guide:

1. **Check the logs**: `docker-compose logs [service-name]`
2. **Verify environment**: Ensure `.env` file has correct values
3. **Clean everything**: `docker system prune -a` and start fresh
4. **Review documentation**: See `DOCKER.md` for detailed setup information
5. **Check system resources**: Ensure adequate memory and disk space

For persistent issues, include the following in your error report:
- Operating system and Docker version
- Complete error logs from failed containers
- Output of `docker-compose ps` and `docker system info`
- Contents of your `.env` file (with passwords redacted)