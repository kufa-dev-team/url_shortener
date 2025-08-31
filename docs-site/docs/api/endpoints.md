---
id: endpoints
title: API Endpoints
---

Base URL: `http://localhost:5135` (Development) | `http://localhost:5000` (Production)

:::info Port Configuration
- **Development (local run):** Uses port 5135 for HTTP and 7127 for HTTPS  
- **Production (Docker):** Uses port 5000 (mapped from internal port 8080)
- **Docker Development:** When running API in Docker, also uses port 5000
:::

:::note Controller Routing
Most endpoints use the `[controller]` route pattern, which means they are prefixed with `/UrlShortener`. 
- Example: `POST /UrlShortener` for creating URLs
- The redirect endpoint follows the same pattern: `GET /UrlShortener/{shortCode}`
- Exception: Some endpoints like `/allActiveUrls` have custom routes
:::

## URL Management Endpoints

### Create Short URL
**POST** `/UrlShortener`

Create a new shortened URL with optional metadata and custom short code.

**Request Body:**
```json
{
  "originalUrl": "https://www.example.com/very/long/url",
  "customShortCode": "my-link",           // Optional: 8 characters
  "expiresAt": "2024-12-31T23:59:59Z",    // Optional
  "title": "Example Website",             // Optional
  "description": "Demo website"           // Optional
}
```

**Response (201 Created):**
```json
{
  "id": 1,
  "shortCode": "my-link",
  "originalUrl": "https://www.example.com/very/long/url",
  "shortUrl": "http://localhost:5135/UrlShortener/my-link",
  "title": "Example Website",
  "description": "Demo website",
  "expiresAt": "2024-12-31T23:59:59Z",
  "isActive": true,
  "clickCount": 0,
  "createdAt": "2024-01-15T10:30:00Z",
  "updatedAt": "2024-01-15T10:30:00Z"
}
```

### Update URL
**PUT** `/UrlShortener`

Update an existing URL mapping.

**Request Body:**
```json
{
  "id": 1,
  "originalUrl": "https://updated-url.com",    // Optional
  "customShortCode": "new-code",               // Optional
  "title": "Updated Title",                    // Optional
  "description": "Updated Description",        // Optional
  "expiresAt": "2025-12-31T23:59:59Z",        // Optional
  "isActive": true                            // Optional
}
```

### Delete URL
**DELETE** `/UrlShortener?id={id}`

Permanently delete a URL mapping by ID.

**Parameters:**
- `id` (query parameter): The numeric ID of the URL mapping to delete

**Validation:**
- ID must be greater than 0

**Response:** 
- `204 No Content` (Success) 
- `400 Bad Request` (Invalid ID)
- `404 Not Found` (URL mapping not found)

### Get All URLs
**GET** `/UrlShortener/GetAll`

Retrieve all URL mappings.

**Response:**
```json
[
  {
    "id": 1,
    "shortCode": "abc123",
    "originalUrl": "https://example.com",
    "shortUrl": "http://localhost:5135/UrlShortener/abc123",
    "title": "Example",
    "description": "Demo",
    "expiresAt": null,
    "isActive": true,
    "clickCount": 15,
    "createdAt": "2024-01-15T10:30:00Z"
  }
]
```

### Get URL by ID
**GET** `/UrlShortener/GetById/{id}`

Retrieve a specific URL mapping by its ID.

### Get Most Popular URLs
**GET** `/UrlShortener/MostClicked/{limit}`

Get the most clicked URLs ordered by click count (descending).

**Parameters:**
- `limit` (path parameter): Number of results to return (1-1000)

**Validation:**
- Limit must be between 1 and 1000

**Response:** Array of URL mappings ordered by click count (descending).

**Example:**
```bash
# Get top 5 most popular URLs
curl http://localhost:5135/UrlShortener/MostClicked/5
```

### Get Active URLs
**GET** `/allActiveUrls`

Retrieve only active (non-expired, enabled) URL mappings.

## Redirect Endpoint

### Redirect to Original URL
**GET** `/UrlShortener/{shortCode}`

Redirects to the original URL and increments click counter.

**Response:** 
- `302 Found` → Redirects to original URL
- `404 Not Found` → Short code not found or inactive

## Administrative Endpoints

### Deactivate Expired URLs
**POST** `/UrlShortener/DeactivateExpired`

Bulk deactivate all expired URLs for maintenance.

**Response:** `204 No Content`

### Purge Cache Entry
**DELETE** `/UrlShortener/admin/cache/{shortCode}`

Remove a specific entry from all cache tiers (redirect + entity cache).

**Response:**
- `204 No Content` → Cache successfully purged
- `404 Not Found` → Entry not found in cache

## Health & Monitoring

### Health Checks
- **GET** `/health/live` → Liveness probe (basic application availability)
- **GET** `/health/ready` → Readiness probe (database + cache connectivity) 
- **GET** `/health` → Comprehensive health status with JSON details

**Example Responses:**

**Liveness (`/health/live`):**
```json
{
  "status": "Healthy",
  "totalDuration": "00:00:00.0001010",
  "entries": {}
}
```

**Readiness (`/health/ready`):**
```json
{
  "status": "Healthy",
  "totalDuration": "00:00:00.1408225",
  "entries": {
    "database": {
      "status": "Healthy",
      "tags": ["db", "postgresql"]
    },
    "redis-cache": {
      "status": "Healthy", 
      "tags": ["cache", "redis"]
    }
  }
}
```

### Metrics
**GET** `/metrics`

Prometheus metrics endpoint for monitoring and observability.

**Format:** Plain text (Prometheus exposition format)  
**Content-Type:** `text/plain; version=0.0.4; charset=utf-8`

## Examples

### Create a Short URL
```bash
curl -X POST http://localhost:5135/UrlShortener \
  -H "Content-Type: application/json" \
  -d '{
    "originalUrl": "https://www.example.com/very/long/url/with/parameters?param1=value1",
    "title": "Example Website",
    "description": "A demonstration URL for testing",
    "expiresAt": "2024-12-31T23:59:59Z"
  }'
```

### Use the Short URL
```bash
curl -L http://localhost:5135/UrlShortener/abc123
# Redirects to original URL and increments click count
```

### Get Popular URLs
```bash
curl http://localhost:5135/UrlShortener/MostClicked/10
```

### Admin Cache Purge
```bash
curl -X DELETE http://localhost:5135/UrlShortener/admin/cache/abc123
```
