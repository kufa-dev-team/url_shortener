---
id: docker-compose
title: Docker Compose Services
---

Services from `compose/docker-compose.dev.yml`:

- PostgreSQL (5432)
- Redis (6379)
- Supabase Studio (8080)
- Redis Commander (8081)

```bash
cd compose
cp .env.example .env

docker-compose -f docker-compose.dev.yml up -d
```
