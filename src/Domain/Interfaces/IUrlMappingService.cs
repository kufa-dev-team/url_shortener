using Domain.Entities;

namespace Domain.Interfaces;

public interface IUrlMappingService
{
    Task<UrlMapping> CreateUrlMappingAsync(UrlMapping urlMapping, string? customShortCode = null);
    Task<UrlMapping?> GetByShortCodeAsync(string shortCode);
    Task<bool> UrlExistsAsync(string shortCode);
    Task<UrlMapping?> GetByIdAsync(int id);
    Task<IEnumerable<UrlMapping>> GetMostClickedUrlsAsync(int limit);
    Task<IEnumerable<UrlMapping>> GetActiveUrlsAsync();
    Task<IEnumerable<UrlMapping>> GetAllUrlsAsync();
    Task<string> RedirectToOriginalUrlAsync(string shortCode);
    Task DeleteUrlAsync(int id);
    Task<UrlMapping> UpdateUrlAsync(UrlMapping urlMapping, string? customShortCode = null);
    Task DeactivateExpiredUrlsAsync();
}