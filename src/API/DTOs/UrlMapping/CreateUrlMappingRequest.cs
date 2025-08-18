namespace API.DTOs.UrlMapping;

public class CreateUrlMappingRequest
{
    /// <summary>
    /// The original URL to be shortened
    /// </summary>
    /// <example>https://www.example.com/very/long/path</example>
    public string OriginalUrl { get; set; } = string.Empty;

    /// <summary>
    /// Custom short code (optional). If not provided, one will be generated automatically
    /// </summary>
    /// <example>my-link</example>
    public string? CustomShortCode { get; set; }

    /// <summary>
    /// Optional expiration date for the shortened URL
    /// </summary>
    /// <example>2024-12-31T23:59:59Z</example>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Optional title for the shortened URL
    /// </summary>
    /// <example>Example Website</example>
    public string? Title { get; set; }

    /// <summary>
    /// Optional description for the shortened URL
    /// </summary>
    /// <example>This is an example website for demonstration purposes</example>
    public string? Description { get; set; }
}