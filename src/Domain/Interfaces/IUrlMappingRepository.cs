
using Domain.Entities;
using Domain.Result;

namespace Domain.Interfaces
{
    public interface IUrlMappingRepository : IRepository<UrlMapping>
    {
        Task<Result<IEnumerable<UrlMapping>>> GetMostClickedAsync(int limit);
        Task<Result<UrlMapping?>> GetByShortCodeAsync(string shortCode);
        Task<Result<IEnumerable<UrlMapping>>> GetActiveAsync();
        Task<Result<bool>> UrlExistsAsync(string shortCode);
        Task<Error?> IncrementClickCountAsync(int id);
        Task<Result<IEnumerable<UrlMapping>>> GetExpiredUrlsAsync();
        Task<Result<int>> DeactivateExpiredUrlsBulkAsync();
    }
}