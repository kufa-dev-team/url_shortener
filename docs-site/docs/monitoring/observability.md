---
id: observability
title: Monitoring & Observability
---

# Monitoring & Observability

Comprehensive monitoring and observability for the URL Shortener application using health checks, Prometheus metrics, and development monitoring stack.

:::info Current Implementation Status
✅ **Implemented:** Health checks, Prometheus metrics endpoint, Docker health checks  
🚧 **In Development:** Custom business metrics, structured logging with Serilog  
📋 **Planned:** Alerting rules, distributed tracing, log aggregation  
:::

## Health Check Endpoints

The application provides multiple health check endpoints for different monitoring scenarios:

### Liveness Probe
**Endpoint:** `GET /health/live`  
**Purpose:** Basic application availability check  
**Use:** Kubernetes/Docker liveness probe  
**Response:** `200 OK` if application is running

```bash
# Development (local run)
curl http://localhost:5135/health/live

# Production (Docker)
curl http://localhost:5000/health/live
```

**Response Format:**
```json
{
  "status": "Healthy",
  "totalDuration": "00:00:00.0001010",
  "entries": {}
}
```

### Readiness Probe  
**Endpoint:** `GET /health/ready`  
**Purpose:** Application readiness (database + cache connectivity)  
**Use:** Kubernetes/Docker readiness probe  
**Dependencies:** PostgreSQL + Redis connectivity

```bash
# Development (local run)
curl http://localhost:5135/health/ready

# Production (Docker)
curl http://localhost:5000/health/ready
```

**Response Format (Success):**
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

### Comprehensive Health Check
**Endpoint:** `GET /health`  
**Purpose:** Detailed health status with JSON response  
**Use:** Monitoring dashboards and alerting

```bash
# Development (local run)
curl http://localhost:5135/health

# Production (Docker)
curl http://localhost:5000/health
```

**Example Response:**
```json
{
  "status": "Healthy",
  "entries": {
    "database": { "status": "Healthy" },
    "redis-cache": { "status": "Healthy" }
  }
}
```

**Possible Status Values:**
- `Healthy` - All systems operational
- `Degraded` - Some non-critical issues (Redis down but database working)
- `Unhealthy` - Critical systems failing (database unavailable)

## Prometheus Metrics

The application exposes detailed metrics via the `/metrics` endpoint in Prometheus format.

### Metrics Endpoint
**URL:** `GET /metrics`  
**Format:** Prometheus exposition format  
**Content-Type:** `text/plain; version=0.0.4; charset=utf-8`

```bash
# Development (local run)
curl http://localhost:5135/metrics

# Production (Docker)
curl http://localhost:5000/metrics
```

### Built-in Metrics

#### HTTP Metrics
```prometheus
# HTTP request duration histogram
http_request_duration_seconds{method="GET",endpoint="/UrlShortener/GetAll",status_code="200"}

# HTTP request count
http_requests_total{method="POST",endpoint="/UrlShortener",status_code="201"}

# Current HTTP requests in progress  
http_requests_in_progress{method="GET",endpoint="/{shortCode}"}
```

#### Application Metrics
```prometheus
# Database connection pool
dotnet_database_connections{state="active"}
dotnet_database_connections{state="idle"}

# .NET runtime metrics
dotnet_gc_collection_count_total{generation="0"}
dotnet_gc_memory_total_available_bytes
dotnet_process_cpu_seconds_total

# Exception counts
dotnet_exceptions_total{type="System.ArgumentException"}
```

#### Custom Business Metrics
:::warning Planned Implementation
These business-specific metrics are planned for future implementation. Currently, the application exposes standard HTTP and runtime metrics only.
:::

```prometheus
# URL operations (planned)
url_redirects_total{status="success"}
url_redirects_total{status="not_found"}
url_creations_total{status="success"}
url_cache_hits_total{tier="redirect"}
url_cache_misses_total{tier="entity"}
```

### Health Check Integration
Health checks are automatically forwarded to Prometheus:

```prometheus
# Health check status (0=unhealthy, 1=healthy)
dotnet_health_check_status{name="database"}
dotnet_health_check_status{name="redis-cache"}

# Health check duration
dotnet_health_check_duration_seconds{name="database"}
```

## Monitoring Stack Setup

:::info Environment-Specific Services
The monitoring stack varies between development and production environments:

- **Development:** Full monitoring stack with Grafana, Prometheus, exporters, and management UIs
- **Production:** Lightweight stack with health checks and metrics endpoints only
:::

### Development Stack (docker-compose.dev.yml)

The `docker/docker-compose.dev.yml` contains the complete monitoring stack:

```yaml
services:
  # Monitoring Services
  prometheus:
    image: prom/prometheus:latest
    ports:
      - "9090:9090"
    volumes:
      - ./monitoring/prometheus.yml:/etc/prometheus/prometheus.yml:ro
    
  grafana:
    image: grafana/grafana:latest
    ports:
      - "3000:3000"
    environment:
      - GF_SECURITY_ADMIN_PASSWORD=admin
      
  postgres-exporter:
    image: prometheuscommunity/postgres_exporter
    ports:
      - "9187:9187"
    environment:
      DATA_SOURCE_NAME: "postgresql://postgres:postgres@postgres:5432/urlshortener?sslmode=disable"
      
  redis-exporter:
    image: oliver006/redis_exporter
    ports:
      - "9121:9121"
    environment:
      REDIS_ADDR: "redis:6379"

  # Container & System Monitoring
  cadvisor:
    image: gcr.io/cadvisor/cadvisor:latest
    ports:
      - "8080:8080"
    # Requires privileged access for container metrics
      
  node-exporter:
    image: prom/node-exporter:latest
    ports:
      - "9100:9100"
    # System metrics collection
    
  # Management UIs
  redis-commander:
    image: ghcr.io/joeferner/redis-commander:latest
    ports:
      - "8081:8081"
    environment:
      HTTP_USER: admin
      HTTP_PASSWORD: admin
      
  pgadmin:
    image: dpage/pgadmin4:latest
    ports:
      - "8082:80"
    environment:
      PGADMIN_DEFAULT_EMAIL: admin@admin.com
      PGLADMIN_DEFAULT_PASSWORD: admin
```

### Production Stack (docker-compose.yml)

Production focuses on core functionality with built-in health checks and metrics:

```yaml
services:
  api:
    # Built-in health checks
    healthcheck:
      test: ["CMD", "wget", "--no-verbose", "--tries=1", "--spider", "http://localhost:8080/health/live"]
      interval: 30s
      timeout: 10s
      retries: 3
    # Exposes /health/live, /health/ready, /health, /metrics endpoints
    
  postgres:
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U postgres -d urlshortener"]
      interval: 10s
      timeout: 5s
      retries: 5
      
  redis:
    healthcheck:
      test: ["CMD", "redis-cli", "-a", "password", "ping"]
      interval: 10s
      timeout: 5s
      retries: 5
```

### Prometheus Targets

**Scrape Configuration** (`docker/monitoring/prometheus.yml`):

```yaml
scrape_configs:
  # Main API service
  - job_name: 'urlshortener-api'
    static_configs:
      - targets: ['host.docker.internal:5000']  # Production Docker
      # - targets: ['host.docker.internal:5135']  # Development local
    metrics_path: '/metrics'
    scrape_interval: 30s

  # PostgreSQL metrics
  - job_name: 'postgres-exporter'
    static_configs:
      - targets: ['postgres-exporter:9187']

  # Redis metrics  
  - job_name: 'redis-exporter'
    static_configs:
      - targets: ['redis-exporter:9121']

  # Container metrics (development only)
  - job_name: 'cadvisor'
    static_configs:
      - targets: ['cadvisor:8080']
      
  # System metrics (development only)  
  - job_name: 'node-exporter'
    static_configs:
      - targets: ['node-exporter:9100']
```

## Grafana Dashboards

### Pre-configured Dashboards

1. **Application Overview**
   - Request rate, response time, error rate
   - Top endpoints by traffic
   - Cache hit rates and performance

2. **Database Performance**
   - Connection pool utilization
   - Query performance and slow queries
   - Transaction rates and deadlocks

3. **Cache Performance**
   - Redis memory usage and evictions
   - Cache hit/miss ratios by tier
   - Key distribution and TTL analysis

4. **System Resources**
   - CPU, memory, disk usage
   - .NET garbage collection metrics
   - Container resource utilization

### Dashboard Access
- **Development Environment**
  - **URL:** http://localhost:3000
  - **Username:** admin
  - **Password:** admin
- **Production Environment**
  - **URL:** http://localhost:3001
  - **Username:** admin  
  - **Password:** admin

### Custom Queries Examples

#### Request Rate by Endpoint
```promql
rate(http_requests_total[5m]) * 60
```

#### Cache Hit Rate
```promql
rate(url_cache_hits_total[5m]) / 
(rate(url_cache_hits_total[5m]) + rate(url_cache_misses_total[5m]))
```

#### Database Connection Pool Usage
```promql
dotnet_database_connections{state="active"} / 
(dotnet_database_connections{state="active"} + dotnet_database_connections{state="idle"}) * 100
```

## Alerting Rules

### Prometheus Alerting Rules

```yaml
# alerts.yml
groups:
  - name: urlshortener_alerts
    rules:
      # High error rate
      - alert: HighErrorRate
        expr: rate(http_requests_total{status_code=~"5.."}[5m]) > 0.1
        for: 2m
        labels:
          severity: warning
        annotations:
          summary: "High error rate detected"
          
      # Database connectivity
      - alert: DatabaseDown
        expr: dotnet_health_check_status{name="database"} == 0
        for: 1m
        labels:
          severity: critical
        annotations:
          summary: "Database is unavailable"
          
      # Cache issues
      - alert: CacheDown
        expr: dotnet_health_check_status{name="redis-cache"} == 0
        for: 2m
        labels:
          severity: warning
        annotations:
          summary: "Redis cache is unavailable"
          
      # Low cache hit rate
      - alert: LowCacheHitRate
        expr: rate(url_cache_hits_total[5m]) / (rate(url_cache_hits_total[5m]) + rate(url_cache_misses_total[5m])) < 0.5
        for: 5m
        labels:
          severity: warning
        annotations:
          summary: "Cache hit rate below 50%"
```

## Logging

### Current Logging Configuration

The application uses the default ASP.NET Core logging framework with the following configuration:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "System.Net.Http.HttpClient": "Warning"
    }
  }
}
```

**Development Environment:**
- **Log Level:** Information (shows detailed application logs)
- **Console Output:** Enabled via ASP.NET Core default providers

**Production Environment:**
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Error",
      "Infrastructure.Services.RedisCacheService": "Warning",
      "Application.Services.CachedUrlMappingService": "Warning"
    }
  }
}
```

:::info Future Enhancement
Structured logging with Serilog is planned for future implementation to provide better log aggregation and searchability.
:::

### Current Log Events

**URL Creation:**
```
[INF] URL shortened: {OriginalUrl} -> {ShortCode} (User: {UserId})
```

**Redirections:**
```
[INF] Redirecting {ShortCode} to {OriginalUrl} (ClickCount: {Count})
```

**Errors:**
```
[ERR] Failed to process request {RequestId}: {Exception}
```

## Performance Monitoring

### Key Performance Indicators (KPIs)

1. **Availability**: 99.9% uptime target
2. **Response Time**: 
   - Redirect: &lt;50ms p95
   - API operations: &lt;200ms p95
3. **Error Rate**: &lt;0.1% of requests
4. **Cache Hit Rate**: &gt;80% for redirects

### Monitoring Queries

#### Response Time Percentiles
```promql
histogram_quantile(0.95, 
  rate(http_request_duration_seconds_bucket[5m])
)
```

#### Throughput by Endpoint
```promql
topk(10, 
  rate(http_requests_total[5m]) * 60
)
```

#### Error Rate Percentage
```promql
(rate(http_requests_total{status_code=~"4..|5.."}[5m]) / 
 rate(http_requests_total[5m])) * 100
```

## Troubleshooting Guide

### Common Monitoring Issues

**Metrics Not Available:**
1. Check `/metrics` endpoint accessibility:
   ```bash
   # Development
   curl http://localhost:5135/metrics
   
   # Production 
   curl http://localhost:5000/metrics
   ```
2. Verify Prometheus can reach the application
3. Check firewall/network configuration
4. Ensure application is running and healthy:
   ```bash
   # Check health first
   curl http://localhost:5000/health
   ```

**High Memory Usage:**
1. Monitor GC metrics: `dotnet_gc_*` (in /metrics output)
2. Check cache memory usage:
   ```bash
   # Development - use Redis Commander UI
   open http://localhost:8081
   
   # Or check Redis directly
   docker exec -it urlshortener-redis redis-cli info memory
   ```
3. Review connection pool: `dotnet_database_connections` metrics

**Database Issues:**
1. Check health endpoint: `/health/ready`
2. Monitor connection metrics in Prometheus (development)
3. Check database connectivity:
   ```bash
   docker exec -it urlshortener-postgres psql -U postgres -d urlshortener -c "SELECT 1;"
   ```
4. Review PostgreSQL logs:
   ```bash
   docker logs urlshortener-postgres
   ```

**Cache Performance:**
1. Check Redis connectivity:
   ```bash
   docker exec -it urlshortener-redis redis-cli ping
   ```
2. Monitor Redis memory and evictions (development only)
3. Review cache configuration in appsettings

### Environment-Specific Troubleshooting

**Development Environment:**
```bash
# Start full monitoring stack
docker-compose -f docker/docker-compose.dev.yml up -d

# Check all services
docker-compose -f docker/docker-compose.dev.yml ps

# Access monitoring tools
echo "Prometheus: http://localhost:9090"
echo "Grafana: http://localhost:3000 (admin/admin)"
echo "Redis Commander: http://localhost:8081 (admin/admin)"
echo "pgAdmin: http://localhost:8082 (admin@admin.com/admin)"
```

**Production Environment:**
```bash
# Check application health
curl http://localhost:5000/health

# View service status
docker-compose -f docker/docker-compose.yml ps

# Check logs
docker-compose -f docker/docker-compose.yml logs api
```

## Quick Setup Guide

### For Development (Full Monitoring Stack)

1. **Start Infrastructure Services:**
   ```bash
   cd docker
   docker-compose -f docker-compose.dev.yml up -d postgres redis
   ```

2. **Start Monitoring Stack:**
   ```bash
   docker-compose -f docker-compose.dev.yml up -d prometheus grafana postgres-exporter redis-exporter
   ```

3. **Run API Locally:**
   ```bash
   cd ..
   dotnet run --project src/API
   # API available at http://localhost:5135
   ```

4. **Verify Setup:**
   ```bash
   # Check health
   curl http://localhost:5135/health
   
   # Check metrics
   curl http://localhost:5135/metrics | head -20
   
   # Access Grafana
   open http://localhost:3000  # admin/admin
   ```

### For Production (Docker Stack)

1. **Set Environment Variables:**
   ```bash
   cp .env.example .env
   # Edit .env with your production values
   ```

2. **Start Production Stack:**
   ```bash
   docker-compose -f docker/docker-compose.yml up -d
   ```

3. **Verify Health:**
   ```bash
   # Wait for services to be healthy
   sleep 30
   curl http://localhost:5000/health
   ```

4. **Monitor with External Tools:**
   - Connect external Prometheus to `http://your-server:5000/metrics`
   - Set up external Grafana with your Prometheus data source
   - Configure alerting with your preferred tool (PagerDuty, Slack, etc.)

## Additional Resources

For more detailed monitoring setup and advanced configurations:

- **[AMP.md](https://github.com/kufa-dev-team/url_shortener/blob/main/docker/AMP.md)** - Comprehensive monitoring setup guide
- **[Docker Testing Guide](https://github.com/kufa-dev-team/url_shortener/blob/main/docker/TESTING.md)** - Health check validation procedures  
- **[Development Setup](../development/local-setup.md)** - Local development environment setup

### Monitoring Best Practices

✅ **Monitor golden signals:** Latency, traffic, errors, saturation  
✅ **Set up proactive alerts** for critical thresholds  
✅ **Use correlation IDs** for request tracing  
✅ **Monitor both infrastructure and application metrics**  
✅ **Regular dashboard reviews** and metric validation  

❌ **Don't over-alert** - focus on actionable alerts  
❌ **Don't ignore false positives** - tune alert thresholds  
❌ **Don't monitor everything** - focus on business impact  
❌ **Don't forget about dependencies** - monitor database and cache health

---

**Documentation Status:** ✅ Updated and verified against source code (August 2025)