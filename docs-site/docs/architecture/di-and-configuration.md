---
id: di-and-configuration
title: DI & Configuration
---

Dependency Injection (DI) is configured in `src/Application/DependencyInjection.cs` and `src/Infrastructure/DependencyInjection.cs`.

- Register repositories and services
- Add DbContext and Redis cache client
- Bind configuration from `appsettings.json`
