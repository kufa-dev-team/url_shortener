---
id: endpoints
title: API Endpoints
---

Base URL: `http://localhost:5135`

- POST `/shorten` — Shorten a URL
  - Body: `{ "originalUrl": "https://example.com/long" }`
  - Response: `{ "shortUrl": "https://short.ly/abc123", "code": "abc123" }`
- GET `/:code` — Redirect to original URL

Example:

```bash
curl -X POST http://localhost:5135/shorten \
  -H "Content-Type: application/json" \
  -d '{"originalUrl": "https://www.example.com/very/long/url"}'
```
