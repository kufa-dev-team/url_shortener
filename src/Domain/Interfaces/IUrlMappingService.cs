using Domain.Entities;
using Domain.Result;

namespace Domain.Interfaces;

public interface IUrlMappingService
{
    Task<Result<UrlMapping>> CreateUrlMappingAsync(UrlMapping urlMapping, string? customShortCode = null);
    Task<Result<UrlMapping?>> GetByShortCodeAsync(string shortCode);
    Task<Result<bool>> UrlExistsAsync(string shortCode);
    Task<Result<UrlMapping?>> GetByIdAsync(int id);
    Task<Result<IEnumerable<UrlMapping>>> GetMostClickedUrlsAsync(int limit);
    Task<Result<IEnumerable<UrlMapping>>> GetActiveUrlsAsync();
    Task<Result<IEnumerable<UrlMapping>>> GetAllUrlsAsync();
    Task<Result<string>> RedirectToOriginalUrlAsync(string shortCode);
    Task<Error?> DeleteUrlAsync(int id);
    Task<Result<UrlMapping>> UpdateUrlAsync(UrlMapping urlMapping, string? customShortCode = null);
    Task<Error?> DeactivateExpiredUrlsAsync();
}