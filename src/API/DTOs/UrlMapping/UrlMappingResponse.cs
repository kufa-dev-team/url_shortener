namespace API.DTOs.UrlMapping;

public class UrlMappingResponse
{
    /// <summary>
    /// Unique identifier for the shortened URL
    /// </summary>
    /// <example>123</example>
    public int Id { get; set; }

    /// <summary>
    /// The original URL that was shortened
    /// </summary>
    /// <example>https://www.example.com/very/long/path</example>
    public string OriginalUrl { get; set; } = string.Empty;

    /// <summary>
    /// The short code used for the URL
    /// </summary>
    /// <example>abc123</example>
    public string ShortCode { get; set; } = string.Empty;

    /// <summary>
    /// The complete shortened URL
    /// </summary>
    /// <example>https://short.ly/abc123</example>
    public string ShortUrl { get; set; } = string.Empty;

    /// <summary>
    /// Number of times the shortened URL has been clicked
    /// </summary>
    /// <example>42</example>
    public int ClickCount { get; set; }

    /// <summary>
    /// When the shortened URL was created
    /// </summary>
    /// <example>2024-01-15T10:30:00Z</example>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the shortened URL expires (if applicable)
    /// </summary>
    /// <example>2024-12-31T23:59:59Z</example>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Whether the shortened URL is currently active
    /// </summary>
    /// <example>true</example>
    public bool IsActive { get; set; }

    /// <summary>
    /// Optional title for the shortened URL
    /// </summary>
    /// <example>Example Website</example>
    public string? Title { get; set; }

    /// <summary>
    /// Optional description for the shortened URL
    /// </summary>
    /// <example>This is an example website</example>
    public string? Description { get; set; }
}

public class UrlMappingStatsResponse
{
    /// <summary>
    /// Unique identifier for the shortened URL
    /// </summary>
    /// <example>123</example>
    public int Id { get; set; }

    /// <summary>
    /// The short code used for the URL
    /// </summary>
    /// <example>abc123</example>
    public string ShortCode { get; set; } = string.Empty;

    /// <summary>
    /// The complete shortened URL
    /// </summary>
    /// <example>https://short.ly/abc123</example>
    public string ShortUrl { get; set; } = string.Empty;

    /// <summary>
    /// Number of times the shortened URL has been clicked
    /// </summary>
    /// <example>42</example>
    public int ClickCount { get; set; }

    /// <summary>
    /// When the shortened URL was created
    /// </summary>
    /// <example>2024-01-15T10:30:00Z</example>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the shortened URL was last clicked
    /// </summary>
    /// <example>2024-01-20T14:22:33Z</example>
    public DateTime? LastClickedAt { get; set; }

    /// <summary>
    /// Whether the shortened URL is currently active
    /// </summary>
    /// <example>true</example>
    public bool IsActive { get; set; }

    /// <summary>
    /// Optional title for the shortened URL
    /// </summary>
    /// <example>Example Website</example>
    public string? Title { get; set; }
}