namespace API.DTOs.UrlMapping;

public class CreateUrlMappingResponse
{
    /// <summary>
    /// The generated short code
    /// </summary>
    /// <example>abc123</example>
    public string ShortCode { get; set; } = string.Empty;

    /// <summary>
    /// The complete shortened URL
    /// </summary>
    /// <example>https://short.ly/abc123</example>
    public string ShortUrl { get; set; } = string.Empty;

    /// <summary>
    /// When the shortened URL expires (if applicable)
    /// </summary>
    /// <example>2025-12-31T23:59:59Z</example>
    public DateTime? ExpiresAt { get; set; }
}