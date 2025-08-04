namespace Domain.Entities;

public class ShortenedUrl : BaseEntity
{
    public string OriginalUrl { get; set; } = string.Empty;
    public string ShortCode { get; set; } = string.Empty;
    public int ClickCount { get; set; } = 0;
    public DateTime? ExpiresAt { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Title { get; set; }
    public string? Description { get; set; }
}