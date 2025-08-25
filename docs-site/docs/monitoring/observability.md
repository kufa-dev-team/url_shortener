---
id: observability
title: Monitoring & Observability
---

Comprehensive monitoring and observability stack using Prometheus, Grafana, and health checks.

## Health Check Endpoints

The application provides multiple health check endpoints for different monitoring scenarios:

### Liveness Probe
**Endpoint:** `GET /health/live`  
**Purpose:** Basic application availability check  
**Use:** Kubernetes/Docker liveness probe  
**Response:** `200 OK` if application is running

```bash
curl http://localhost:5135/health/live
# Response: Healthy
```

### Readiness Probe  
**Endpoint:** `GET /health/ready`  
**Purpose:** Application readiness (database + cache connectivity)  
**Use:** Kubernetes/Docker readiness probe  
**Dependencies:** PostgreSQL + Redis connectivity

```bash
curl http://localhost:5135/health/ready
# Response: Healthy (if all dependencies available)
```

### Comprehensive Health Check
**Endpoint:** `GET /health`  
**Purpose:** Detailed health status with JSON response  
**Use:** Monitoring dashboards and alerting

```bash
curl http://localhost:5135/health
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
- `Degraded` - Some non-critical issues
- `Unhealthy` - Critical systems failing

## Prometheus Metrics

The application exposes detailed metrics via the `/metrics` endpoint in Prometheus format.

### Metrics Endpoint
**URL:** `GET /metrics`  
**Format:** Prometheus exposition format  
**Content-Type:** `text/plain; version=0.0.4; charset=utf-8`

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
```prometheus
# URL operations
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

### Docker Compose Configuration

The `docker/monitoring/` folder contains the complete monitoring stack:

```yaml
# From docker-compose.yml (monitoring services)
services:
  prometheus:
    image: prom/prometheus:v2.47.0
    ports:
      - "9090:9090"
    volumes:
      - ./monitoring/prometheus.yml:/etc/prometheus/prometheus.yml
    
  grafana:
    image: grafana/grafana:10.1.0
    ports:
      - "3001:3000"
    volumes:
      - ./monitoring/grafana:/etc/grafana/provisioning
    environment:
      - GF_SECURITY_ADMIN_PASSWORD=admin
      
  postgres-exporter:
    image: prometheuscommunity/postgres-exporter:v0.13.0
    environment:
      DATA_SOURCE_NAME: "postgresql://postgres:postgres@postgres:5432/urlshortener?sslmode=disable"
      
  redis-exporter:
    image: oliver006/redis_exporter:v1.53.0
    environment:
      REDIS_ADDR: "redis://redis:6379"
```

### Prometheus Targets

**Scrape Configuration** (`docker/monitoring/prometheus.yml`):

```yaml
scrape_configs:
  # Main API service
  - job_name: 'urlshortener-api'
    static_configs:
      - targets: ['host.docker.internal:5000']
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

  # Container metrics
  - job_name: 'cadvisor'
    static_configs:
      - targets: ['cadvisor:8080']
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

### Structured Logging Configuration

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "System.Net.Http.HttpClient": "Warning"
    }
  },
  "Serilog": {
    "Using": [ "Serilog.Sinks.Console", "Serilog.Sinks.File" ],
    "MinimumLevel": "Information",
    "WriteTo": [
      { "Name": "Console" },
      {
        "Name": "File",
        "Args": { 
          "path": "/app/logs/urlshortener-.log",
          "rollingInterval": "Day"
        }
      }
    ]
  }
}
```

### Key Log Events

**URL Creation:**
```
[INF] URL shortened: {OriginalUrl} -> {ShortCode} (User: {UserId})
```

**Redirections:**
```
[INF] Redirecting {ShortCode} to {OriginalUrl} (ClickCount: {Count})
```

**Cache Operations:**
```
[DBG] Cache {Operation}: {Key} (Hit: {IsHit}, TTL: {TTL}s)
```

**Errors:**
```
[ERR] Failed to process request {RequestId}: {Exception}
```

## Performance Monitoring

### Key Performance Indicators (KPIs)

1. **Availability**: 99.9% uptime target
2. **Response Time**: 
   - Redirect: <50ms p95
   - API operations: <200ms p95
3. **Error Rate**: <0.1% of requests
4. **Cache Hit Rate**: >80% for redirects

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
1. Check `/metrics` endpoint accessibility
2. Verify Prometheus can reach the application
3. Check firewall/network configuration

**High Memory Usage:**
1. Monitor GC metrics: `dotnet_gc_*`
2. Check cache memory: Redis INFO memory
3. Review connection pool: `dotnet_database_connections`

**Database Issues:**
1. Check health endpoint: `/health/ready`
2. Monitor connection metrics
3. Review slow query logs

**Cache Performance:**
1. Check hit/miss ratios
2. Monitor Redis memory and evictions
3. Review TTL distribution

### Monitoring Best Practices

✅ **Monitor golden signals:** Latency, traffic, errors, saturation
✅ **Set up proactive alerts** for critical thresholds
✅ **Use structured logging** with correlation IDs
✅ **Monitor both infrastructure and application metrics**
✅ **Regular dashboard reviews** and metric validation

❌ **Don't over-alert** - focus on actionable alerts
❌ **Don't ignore false positives** - tune alert thresholds
❌ **Don't monitor everything** - focus on business impact