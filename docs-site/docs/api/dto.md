---
id: dto
title: DTOs
---

Common request/response data transfer objects used by the API. See `src/API/DTOs/UrlMapping`.

```csharp
// Example
public record ShortenUrlRequest(string OriginalUrl);

public record ShortenUrlResponse(string ShortUrl, string Code);
```
