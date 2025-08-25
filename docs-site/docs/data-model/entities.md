---
id: entities
title: Entities
---

Core entities live in `src/Domain/Entities`.

```csharp
public abstract class BaseEntity
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

public class UrlMapping : BaseEntity
{
    public required string OriginalUrl { get; set; }
    public required string ShortCode { get; set; }
    public DateTime? ExpiresAt { get; set; }
}
```
