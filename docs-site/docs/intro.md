---
sidebar_position: 1
---

# URL Shortener Documentation

Welcome to the **URL Shortener** project documentation! This is a comprehensive guide for a high-performance ASP.NET Core 9.0 URL shortening service.

## 🎯 What You'll Learn

This documentation covers everything from basic setup to advanced caching patterns and monitoring:

- **Complete API Reference** - All endpoints with examples
- **Advanced Caching** - Hybrid dual-tier Redis caching system
- **Clean Architecture** - Modern .NET development patterns  
- **Monitoring & Observability** - Prometheus + Grafana setup
- **Development Workflow** - Docker-based local development

## 🚀 Quick Start

Get up and running in minutes:

1. **Clone the repository**
2. **Start services**: `docker-compose -f docker/docker-compose.dev.yml up -d`
3. **Apply migrations**: `./scripts/migrate.sh`
4. **Run API**: `dotnet run --project src/API`

Visit http://localhost:5135/swagger to explore the API!

## 📚 Documentation Structure

### 🏗️ [Architecture](./architecture/clean-architecture.md)
Learn about the Clean Architecture implementation, dependency injection, and project structure.

### 📡 [API Reference](./api/endpoints.md)  
Complete endpoint documentation with request/response examples and error handling.

### 🚀 [Caching](./caching/patterns.md)
Advanced hybrid caching strategies using Redis for optimal performance.

### 💾 [Data Model](./data-model/entities.md)
Database entities, relationships, and Entity Framework Core implementation.

### 👨‍💻 [Development](./development/local-setup.md)
Local development setup, migrations, testing, and debugging.

### 📊 [Monitoring](./monitoring/observability.md)
Prometheus metrics, Grafana dashboards, and health check configuration.

## 🔧 Key Features

### Performance Optimizations
- **Hybrid Dual-Tier Caching** - 60-80% memory reduction
- **Connection Pooling** - Optimized database connections
- **Bulk Operations** - Efficient maintenance tasks
- **Asynchronous Processing** - High-throughput request handling

### Modern Development Practices
- **Result Pattern** - Functional error handling
- **FluentValidation** - Robust input validation
- **Structured Logging** - Comprehensive observability
- **Health Checks** - Application and dependency monitoring

### Enterprise Features
- **Prometheus Integration** - Metrics collection and alerting
- **Grafana Dashboards** - Visualization and monitoring
- **Docker Compose** - Containerized development environment
- **Database Migrations** - Automated schema management

## 🎓 Perfect for Learning

This project demonstrates:
- **Modern C#** features (records, pattern matching, nullable reference types)
- **Clean Architecture** principles and implementation
- **Advanced Caching** patterns and strategies  
- **Monitoring & Observability** best practices
- **High-Performance** web API development

## 💡 Need Help?

- **Start Here**: [Local Setup Guide](./development/local-setup.md)
- **API Basics**: [Endpoint Documentation](./api/endpoints.md)  
- **Understanding the Code**: [Architecture Overview](./architecture/clean-architecture.md)
- **Performance Deep Dive**: [Caching Patterns](./caching/patterns.md)

---

Ready to dive in? Check out the [**Overview**](./overview.md) for a comprehensive introduction to the project!
