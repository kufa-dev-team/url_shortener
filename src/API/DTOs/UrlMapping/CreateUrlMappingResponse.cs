using System.ComponentModel.DataAnnotations;

namespace API.DTOs.UrlMapping;

public class CreateUrlMappingResponse
{
    [Display(Name = "ID", Description = "Unique system-generated identifier")]
    public int Id { get; set; }
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
    [DataType(DataType.DateTime)]
    public DateTime? ExpiresAt { get; set; }

    [Required]
    [StringLength(500)]
    public string? Title { get; set; }

    [Required]
    [StringLength(2000)]
    public string? Description { get; set; }

    [Required]
    [Url]
    public string? OriginalUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int ClickCount { get; set; }
    public bool IsActive { get; set; }
}