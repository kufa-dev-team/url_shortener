---
id: entities
title: Entities
---

Core domain entities located in `src/Domain/Entities/`. These represent the business objects and database tables.

## BaseEntity

All entities inherit from `BaseEntity` which provides common auditing fields:

```csharp
public abstract class BaseEntity
{
    public int Id { get; set; }                              // Primary key
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;  // Creation timestamp
    public DateTime? UpdatedAt { get; set; }                 // Last modification timestamp
}
```

## UrlMapping

The primary entity representing a shortened URL mapping:

```csharp
public class UrlMapping : BaseEntity
{
    public string OriginalUrl { get; set; } = string.Empty;  // The original long URL
    public string ShortCode { get; set; } = string.Empty;    // The short code (8 characters)
    public int ClickCount { get; set; } = 0;                 // Number of times clicked
    public DateTime? ExpiresAt { get; set; }                 // Optional expiration date
    public bool IsActive { get; set; } = true;               // Whether URL is active
    public string? Title { get; set; }                       // Optional title/name
    public string? Description { get; set; }                 // Optional description
}
```

### Property Details

| Property | Type | Description | Default | Required |
|----------|------|-------------|---------|----------|
| `Id` | `int` | Primary key, auto-generated | Auto | ✅ |
| `OriginalUrl` | `string` | The original long URL to redirect to | `""` | ✅ |
| `ShortCode` | `string` | Unique 8-character identifier | `""` | ✅ |
| `ClickCount` | `int` | Number of times the short URL was accessed | `0` | ✅ |
| `ExpiresAt` | `DateTime?` | When the URL expires (nullable) | `null` | ❌ |
| `IsActive` | `bool` | Whether the URL mapping is active | `true` | ✅ |
| `Title` | `string?` | Optional human-readable title | `null` | ❌ |
| `Description` | `string?` | Optional description | `null` | ❌ |
| `CreatedAt` | `DateTime` | When the record was created | `DateTime.UtcNow` | ✅ |
| `UpdatedAt` | `DateTime?` | When the record was last updated | `null` | ❌ |

### Business Rules

1. **ShortCode Uniqueness**: Each short code must be unique across all mappings
2. **URL Validation**: Original URLs must be valid HTTP/HTTPS URLs
3. **Expiration Logic**: URLs with `ExpiresAt` in the past become inactive
4. **Click Tracking**: `ClickCount` is incremented atomically on each redirect
5. **Soft Deletion**: URLs are deactivated rather than deleted to preserve analytics

### Database Schema

```sql
CREATE TABLE "UrlMappings" (
    "Id" SERIAL PRIMARY KEY,
    "OriginalUrl" TEXT NOT NULL,
    "ShortCode" VARCHAR(8) NOT NULL UNIQUE,
    "ClickCount" INTEGER NOT NULL DEFAULT 0,
    "ExpiresAt" TIMESTAMP WITH TIME ZONE NULL,
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE,
    "Title" TEXT NULL,
    "Description" TEXT NULL,
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "UpdatedAt" TIMESTAMP WITH TIME ZONE NULL
);

-- Indexes for performance
CREATE INDEX "IX_UrlMappings_ShortCode" ON "UrlMappings" ("ShortCode");
CREATE INDEX "IX_UrlMappings_IsActive" ON "UrlMappings" ("IsActive");
CREATE INDEX "IX_UrlMappings_ClickCount" ON "UrlMappings" ("ClickCount" DESC);
CREATE INDEX "IX_UrlMappings_ExpiresAt" ON "UrlMappings" ("ExpiresAt") WHERE "ExpiresAt" IS NOT NULL;
```

## Entity Lifecycle

### Creation
1. New `UrlMapping` entity created with required fields
2. `CreatedAt` set to current UTC time
3. `UpdatedAt` set to current UTC time  
4. `IsActive` defaults to `true`
5. `ClickCount` defaults to `0`
6. Short code generated (8 characters) or validated if custom

### Updates
1. `UpdatedAt` timestamp updated on any modification
2. Cache invalidation triggered for updated entities
3. Historical click counts preserved

### Expiration
1. URLs with `ExpiresAt` in the past are considered expired
2. Expired URLs can be bulk deactivated via admin endpoint
3. Expired URLs return 404 on redirect attempts

### Soft Deletion
1. Entities marked as `IsActive = false` rather than physical deletion
2. Analytics and historical data preserved
3. Short codes can be reused after sufficient time

## Usage Examples

### Entity Framework Query Examples

```csharp
// Find active URLs
var activeUrls = await context.UrlMappings
    .Where(u => u.IsActive && (!u.ExpiresAt.HasValue || u.ExpiresAt > DateTime.UtcNow))
    .ToListAsync();

// Get most popular URLs
var popularUrls = await context.UrlMappings
    .Where(u => u.IsActive)
    .OrderByDescending(u => u.ClickCount)
    .Take(10)
    .ToListAsync();

// Bulk deactivate expired URLs
var expiredCount = await context.UrlMappings
    .Where(u => u.IsActive && u.ExpiresAt.HasValue && u.ExpiresAt <= DateTime.UtcNow)
    .ExecuteUpdateAsync(setters => setters.SetProperty(u => u.IsActive, false));
```
