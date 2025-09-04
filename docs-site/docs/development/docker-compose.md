---
id: docker-compose
title: Docker Compose Services
---

Services from `docker/docker-compose.dev.yml`:

- PostgreSQL (5432)
- Redis (6379)
- pgAdmin (8082)
- Redis Commander (8081)
- cAdvisor (8080)

```bash
cd docker
cp .env.example .env

docker-compose -f docker-compose.dev.yml up -d
```
