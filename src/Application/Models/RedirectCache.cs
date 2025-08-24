namespace Application.Models;

/// <summary>
/// Lightweight cache model optimized for redirect operations.
/// Contains only essential data needed for URL redirection.
/// </summary>
public class RedirectCache
{
    /// <summary>
    /// The original URL to redirect to
    /// </summary>
    public string OriginalUrl { get; set; } = string.Empty;
    
    /// <summary>
    /// Whether the URL is active and can be redirected
    /// </summary>
    public bool IsActive { get; set; }
    
    /// <summary>
    /// Optional expiration date for the URL
    /// </summary>
    public DateTime? ExpiresAt { get; set; }
    
    /// <summary>
    /// URL ID for click count incrementing
    /// </summary>
    public int Id { get; set; }
}