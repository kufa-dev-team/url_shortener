
using Domain.Entities;

namespace Domain.Interfaces
{
    public interface IUrlMappingRepository : IRepository<UrlMapping>
    {
        Task<IEnumerable<UrlMapping>> GetMostClickedAsync(int limit);
        Task<UrlMapping?> GetByShortCodeAsync(string shortCode);
        Task<IEnumerable<UrlMapping>> GetActiveAsync();
        Task<bool> UrlExistsAsync(string shortCode);
        Task IncrementClickCountAsync(int id);

    }
}