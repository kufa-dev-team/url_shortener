---
id: local-setup
title: Local Development
---

Install dependencies and run the API:

```bash
# Build
 dotnet build

# Run
 dotnet run --project src/API
```

Troubleshooting:
- Ensure PostgreSQL and Redis are reachable
- Check connection strings in `src/API/appsettings.Development.json`
