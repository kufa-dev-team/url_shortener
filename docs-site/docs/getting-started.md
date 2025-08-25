---
id: getting-started
title: Getting Started
slug: /getting-started
---

This guide helps you run the project locally with Docker or directly with the .NET SDK.

## Prerequisites

- .NET 9 SDK
- Docker Desktop (macOS/Windows) or Docker Engine (Linux)

## Quick Start with Docker

```bash
cd compose
cp .env.example .env

docker-compose -f docker-compose.dev.yml up -d

docker-compose -f docker-compose.dev.yml ps
```

Run the API:

```bash
cd ..
dotnet build

dotnet run --project src/API
```

Visit http://localhost:5135

## Local (without Docker)

- Install PostgreSQL and Redis locally
- Update connection strings in `src/API/appsettings.json`
- Run `dotnet run --project src/API`
