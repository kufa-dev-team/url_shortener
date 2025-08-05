using Domain.Entities;

namespace Application.Services;

public interface IUrlShortenerService
{
    Task<UrlMapping> CreateShortUrlAsync(string originalUrl);
    Task<UrlMapping?> GetByShortCodeAsync(string shortCode);
    Task<IEnumerable<UrlMapping>> GetAllUrlsAsync();
    Task<string> RedirectToOriginalUrlAsync(string shortCode);
}