
using Domain.Entities;

namespace Domain.Interfaces
{
    public interface IUrlMappingRepository : IRepository<UrlMapping>
    {
        Task<IEnumerable<UrlMapping>> GetMostClickedAsync(int limit);
        Task<IEnumerable<UrlMapping>> GetActiveAsync();


    }
}