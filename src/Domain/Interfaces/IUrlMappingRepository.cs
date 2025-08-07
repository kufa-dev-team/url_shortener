using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Domain.Entities;

namespace Domain.Interfaces
{
    public interface IUrlMappingRepository
    {
        //get tasks
        Task<IEnumerable<UrlMapping>> GetAllAsync();
        Task<UrlMapping?> GetByShortUrlAsync(string ShortCode);
        Task<UrlMapping?> GetByTitleAsync(string Title);
        Task<IEnumerable<UrlMapping>> GetMostClickedAsync(int limit);
        Task<IEnumerable<UrlMapping>> GetActiveAsync();

        //add, update, delete tasks
        Task<UrlMapping> AddAsync(UrlMapping urlMapping);
        Task UpdateAsync(UrlMapping urlMapping);
        Task DeleteAsync(String ShortCode);

    }
}