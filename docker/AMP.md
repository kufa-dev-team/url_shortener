# Monitoring Setup for URL Shortener

This directory contains monitoring configuration for the URL Shortener application using Prometheus and Grafana.

## 🎯 Overview

Complete monitoring stack with:
- **Prometheus** - Metrics collection and storage
- **Grafana** - Visualization dashboards
- **PostgreSQL Exporter** - Database metrics
- **Redis Exporter** - Cache metrics
- **API Metrics** - Application performance metrics

## 📊 Architecture

```
┌─────────────────┐    ┌──────────────────┐    ┌─────────────────┐
│   API Service   │───▶│   Prometheus     │───▶│    Grafana      │
│ (localhost:5000)│    │  (localhost:9090)│    │(localhost:3000) │
└─────────────────┘    └──────────────────┘    └─────────────────┘
                              │
                              ▼
                    ┌──────────────────┐
                    │    Exporters     │
                    │ ┌──────────────┐ │
                    │ │ PostgreSQL   │ │
                    │ │ (port 9187)  │ │
                    │ └──────────────┘ │
                    │ ┌──────────────┐ │
                    │ │    Redis     │ │
                    │ │ (port 9121)  │ │
                    │ └──────────────┘ │
                    └──────────────────┘
```

## 🚀 Quick Start

### 1. Start Infrastructure Services
```bash
# Start all monitoring services
docker-compose -f docker/docker-compose.dev.yml up -d

# Check services status
docker-compose -f docker/docker-compose.dev.yml ps
```

### 2. Start API Service
```bash
# Run API locally (required for metrics)
cd src/API
dotnet run

# Verify API metrics endpoint
curl http://localhost:5000/metrics
```

### 3. Access Monitoring Services

| Service | URL | Credentials |
|---------|-----|-------------|
| **Prometheus** | http://localhost:9090 | No auth |
| **Grafana** | http://localhost:3000 | admin/admin |
| **PostgreSQL Exporter** | http://localhost:9187/metrics | No auth |
| **Redis Exporter** | http://localhost:9121/metrics | No auth |

## 📈 Available Metrics

### API Metrics (`/metrics`)
- HTTP request duration
- Request count by endpoint
- Response status codes
- Database connection pool
- Cache hit/miss ratios

### PostgreSQL Metrics (`:9187/metrics`)
- Database connections
- Query performance
- Table sizes
- Index usage
- Lock statistics

### Redis Metrics (`:9121/metrics`)
- Memory usage
- Key statistics
- Command statistics
- Replication info
- Persistence metrics

## 🔧 Configuration Files

### prometheus.yml
Main Prometheus configuration with scrape targets:

```yaml
scrape_configs:
  - job_name: 'urlshortener-api'
    static_configs:
      - targets: ['host.docker.internal:5000']  # API running locally
  
  - job_name: 'postgres-exporter'
    static_configs:
      - targets: ['postgres-exporter:9187']     # PostgreSQL metrics
  
  - job_name: 'redis-exporter'
    static_configs:
      - targets: ['redis-exporter:9121']        # Redis metrics
```

### Docker Services
- **prometheus**: Metrics storage and collection
- **grafana**: Visualization dashboards
- **postgres-exporter**: PostgreSQL metrics exporter
- **redis-exporter**: Redis metrics exporter

## 🎛️ Grafana Dashboards

### Pre-configured Dashboards
1. **API Performance Dashboard**
   - Request rates and latencies
   - Error rates by endpoint
   - Response time percentiles

2. **Database Dashboard**
   - Connection pool status
   - Query performance
   - Table and index statistics

3. **Cache Dashboard**
   - Redis memory usage
   - Hit/miss ratios
   - Key expiration metrics

### Custom Dashboard Creation
1. Access Grafana at http://localhost:3000
2. Login with `admin/admin`
3. Go to **Dashboards** → **New** → **Import**
4. Use Prometheus as data source: `http://prometheus:9090`

## 🔍 Monitoring Queries

### Useful Prometheus Queries

#### API Performance
```promql
# Request rate per second
rate(http_requests_total[5m])

# 95th percentile response time
histogram_quantile(0.95, rate(http_request_duration_seconds_bucket[5m]))

# Error rate percentage
rate(http_requests_total{status=~"5.."}[5m]) / rate(http_requests_total[5m]) * 100
```

#### Database Performance
```promql
# Active connections
pg_stat_database_numbackends

# Database size
pg_database_size_bytes

# Query execution time
rate(pg_stat_statements_total_time[5m]) / rate(pg_stat_statements_calls[5m])
```

#### Redis Performance
```promql
# Memory usage
redis_memory_used_bytes

# Cache hit ratio
rate(redis_keyspace_hits_total[5m]) / (rate(redis_keyspace_hits_total[5m]) + rate(redis_keyspace_misses_total[5m])) * 100

# Connected clients
redis_connected_clients
```

## 🚨 Alerting Rules

### Critical Alerts
- API response time > 1 second
- Error rate > 5%
- Database connections > 80% of max
- Redis memory usage > 90%

### Warning Alerts
- API response time > 500ms
- Error rate > 1%
- Database connections > 60% of max
- Redis memory usage > 70%

## 🛠️ Troubleshooting

### Common Issues

#### API Metrics Not Available
**Problem**: `urlshortener-api` shows as DOWN in Prometheus

**Solutions**:
```bash
# 1. Verify API is running
curl http://localhost:5000/health

# 2. Check metrics endpoint
curl http://localhost:5000/metrics

# 3. Restart Prometheus
docker-compose -f docker/docker-compose.dev.yml restart prometheus
```

#### Exporter Connection Issues
**Problem**: PostgreSQL or Redis exporters show connection errors

**Solutions**:
```bash
# Check database connectivity
docker exec -it urlshortener-postgres psql -U postgres -d urlshortener -c "SELECT 1;"

# Check Redis connectivity
docker exec -it urlshortener-redis redis-cli ping

# Restart exporters
docker-compose -f docker/docker-compose.dev.yml restart postgres-exporter redis-exporter
```

#### Grafana Dashboard Issues
**Problem**: No data in Grafana dashboards

**Solutions**:
1. Verify Prometheus data source: `http://prometheus:9090`
2. Check Prometheus targets are UP
3. Verify query syntax in dashboard panels

### Health Check Commands
```bash
# Check all services status
docker-compose -f docker/docker-compose.dev.yml ps

# View service logs
docker-compose -f docker/docker-compose.dev.yml logs prometheus
docker-compose -f docker/docker-compose.dev.yml logs grafana
docker-compose -f docker/docker-compose.dev.yml logs postgres-exporter
docker-compose -f docker/docker-compose.dev.yml logs redis-exporter

# Test metrics endpoints
curl http://localhost:9090/targets          # Prometheus targets
curl http://localhost:9187/metrics          # PostgreSQL metrics
curl http://localhost:9121/metrics          # Redis metrics
curl http://localhost:5000/metrics          # API metrics
```

## 📚 Additional Resources

### Prometheus
- [Prometheus Documentation](https://prometheus.io/docs/)
- [PromQL Query Language](https://prometheus.io/docs/prometheus/latest/querying/)
- [Recording Rules](https://prometheus.io/docs/prometheus/latest/configuration/recording_rules/)

### Grafana
- [Grafana Documentation](https://grafana.com/docs/)
- [Dashboard Best Practices](https://grafana.com/docs/grafana/latest/best-practices/)
- [Prometheus Data Source](https://grafana.com/docs/grafana/latest/datasources/prometheus/)

### Exporters
- [PostgreSQL Exporter](https://github.com/prometheus-community/postgres_exporter)
- [Redis Exporter](https://github.com/oliver006/redis_exporter)
- [.NET Metrics](https://docs.microsoft.com/en-us/dotnet/core/diagnostics/metrics)

## 🎯 Next Steps

1. **Set up Alerting**: Configure Prometheus AlertManager
2. **Add More Dashboards**: Create custom business metrics dashboards
3. **Log Aggregation**: Add ELK stack or Loki for log monitoring
4. **Distributed Tracing**: Implement Jaeger or Zipkin
5. **Performance Testing**: Use monitoring during load tests

---

**Happy Monitoring!** 📊✨