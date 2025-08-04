using Domain.Entities;

namespace Application.Services;

public interface IUrlShortenerService
{
    Task<ShortenedUrl> CreateShortUrlAsync(string originalUrl);
    Task<ShortenedUrl?> GetByShortCodeAsync(string shortCode);
    Task<IEnumerable<ShortenedUrl>> GetAllUrlsAsync();
    Task<string> RedirectToOriginalUrlAsync(string shortCode);
}