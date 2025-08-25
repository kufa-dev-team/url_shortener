---
id: dto
title: Data Transfer Objects (DTOs)
---

All API request and response objects used by the URL Shortener service. Located in `src/API/DTOs/UrlMapping/`.

## Request DTOs

### CreateUrlMappingRequest
Used for creating new shortened URLs.

```csharp
public class CreateUrlMappingRequest
{
    /// <summary>
    /// The original URL to be shortened (required)
    /// </summary>
    public string OriginalUrl { get; set; } = string.Empty;

    /// <summary>
    /// Custom short code (optional). Must be exactly 8 characters if provided.
    /// If not provided, one will be generated automatically.
    /// </summary>
    public string? CustomShortCode { get; set; }

    /// <summary>
    /// Optional expiration date. Must be in the future.
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Optional title for the shortened URL
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Optional description for the shortened URL
    /// </summary>
    public string? Description { get; set; }
}
```

**Example:**
```json
{
  "originalUrl": "https://www.example.com/very/long/path/to/resource?param=value",
  "customShortCode": "my-link1",
  "expiresAt": "2024-12-31T23:59:59Z",
  "title": "Example Resource",
  "description": "This is an example resource for demonstration"
}
```

### UpdateUrlMappingRequest
Used for updating existing URL mappings.

```csharp
public class UpdateUrlMappingRequest
{
    public int Id { get; set; }                    // Required: ID of URL to update
    public string? OriginalUrl { get; set; }       // Optional: New original URL
    public string? CustomShortCode { get; set; }   // Optional: New short code
    public string? Title { get; set; }             // Optional: New title
    public string? Description { get; set; }       // Optional: New description
    public DateTime? ExpiresAt { get; set; }       // Optional: New expiration
    public bool IsActive { get; set; }             // Optional: Active status
}
```

## Response DTOs

### CreateUrlMappingResponse
Response when creating a new shortened URL.

```csharp
public class CreateUrlMappingResponse
{
    public int Id { get; set; }
    public string ShortCode { get; set; } = string.Empty;
    public string OriginalUrl { get; set; } = string.Empty;
    public string ShortUrl { get; set; } = string.Empty;      // Full short URL
    public string? Title { get; set; }
    public string? Description { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public bool IsActive { get; set; }
    public int ClickCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

**Example Response:**
```json
{
  "id": 1,
  "shortCode": "abc12345",
  "originalUrl": "https://www.example.com/very/long/path",
  "shortUrl": "http://localhost:5135/abc12345",
  "title": "Example Resource",
  "description": "This is an example resource",
  "expiresAt": "2024-12-31T23:59:59Z",
  "isActive": true,
  "clickCount": 0,
  "createdAt": "2024-01-15T10:30:00Z",
  "updatedAt": "2024-01-15T10:30:00Z"
}
```

### UrlMappingResponse
Standard response for URL mapping queries (GetById, GetAll, etc.).

```csharp
public class UrlMappingResponse
{
    public int Id { get; set; }
    public string ShortCode { get; set; } = string.Empty;
    public string OriginalUrl { get; set; } = string.Empty;
    public string ShortUrl { get; set; } = string.Empty;
    public string? Title { get; set; }
    public string? Description { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public bool IsActive { get; set; }
    public int ClickCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
```

## Validation Rules

### URL Validation
- **OriginalUrl**: Must be a valid HTTP/HTTPS URL
- **CustomShortCode**: Exactly 8 characters, alphanumeric
- **ExpiresAt**: Must be in the future if provided
- **Title/Description**: Optional string fields

### Error Responses
All endpoints return consistent error responses:

```json
{
  "status": 400,
  "message": "Custom short code must be 8 characters long."
}
```

Common HTTP status codes:
- `200 OK` - Successful retrieval
- `201 Created` - Successful creation
- `204 No Content` - Successful deletion/deactivation
- `400 Bad Request` - Validation error
- `404 Not Found` - Resource not found
- `500 Internal Server Error` - Server error

## Usage Examples

### Create with all optional fields:
```bash
curl -X POST http://localhost:5135/UrlShortener \
  -H "Content-Type: application/json" \
  -d '{
    "originalUrl": "https://github.com/microsoft/dotnet",
    "customShortCode": "dotnet01",
    "title": "Microsoft .NET Repository", 
    "description": "Official .NET repository on GitHub",
    "expiresAt": "2025-12-31T23:59:59Z"
  }'
```

### Simple create (minimal):
```bash
curl -X POST http://localhost:5135/UrlShortener \
  -H "Content-Type: application/json" \
  -d '{"originalUrl": "https://docs.microsoft.com/dotnet"}'
```

### Update existing URL:
```bash
curl -X PUT http://localhost:5135/UrlShortener \
  -H "Content-Type: application/json" \
  -d '{
    "id": 1,
    "title": "Updated Title",
    "description": "Updated description",
    "isActive": true
  }'
```
