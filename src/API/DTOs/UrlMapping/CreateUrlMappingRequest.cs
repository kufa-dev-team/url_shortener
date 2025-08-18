namespace API.DTOs.UrlMapping;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

// Request DTOs
public class CreateUrlMappingRequest
{
    
    /// <summary>
    /// The original URL to be shortened
    /// </summary>
    /// <example>https://www.example.com/very/long/path</example>
    [Required(ErrorMessage = "Original URL is required")]
    [Url(ErrorMessage = "Please provide a valid URL")]
    [MaxLength(2048, ErrorMessage = "URL cannot exceed 2048 characters")]
    public string OriginalUrl { get; set; } = string.Empty;

    /// <summary>
    /// Custom short code (optional). If not provided, one will be generated automatically
    /// </summary>
    /// <example>my-link</example>
    [RegularExpression(@"^[a-zA-Z0-9_-]+$", ErrorMessage = "Short code can only contain letters, numbers, hyphens, and underscores")]
    //[MinLength(3, ErrorMessage = "Custom short code must be at least 3 characters")]
    [MaxLength(20, ErrorMessage = "Custom short code cannot exceed 20 characters")]
    public string? CustomShortCode { get; set; }

    /// <summary>
    /// Optional expiration date for the shortened URL
    /// </summary>
    /// <example>2024-12-31T23:59:59Z</example>
    [DataType(DataType.DateTime)]
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Optional title for the shortened URL
    /// </summary>
    /// <example>Example Website</example>
    [MaxLength(200, ErrorMessage = "Title cannot exceed 200 characters")]
    public string? Title { get; set; }

    /// <summary>
    /// Optional description for the shortened URL
    /// </summary>
    /// <example>This is an example website for demonstration purposes</example>
    [MaxLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
    public string? Description { get; set; }
}