namespace API.DTOs.UrlMapping;

public class UpdateUrlMappingRequest
{
    public int Id { get; set; }
    public string? CustomShortCode { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public required string? OriginalUrl { get; set; } = null!;
    public bool IsActive { get; set; } = true;
    public DateTime? ExpiresAt { get; set; }
}