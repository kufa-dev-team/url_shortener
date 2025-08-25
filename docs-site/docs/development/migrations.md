---
id: migrations
title: Database Migrations
---

Apply EF Core migrations:

```bash
dotnet ef migrations add Init --project src/Infrastructure --startup-project src/API

dotnet ef database update --project src/Infrastructure --startup-project src/API
```

Make sure the EF tools are installed: `dotnet tool install --global dotnet-ef`.
