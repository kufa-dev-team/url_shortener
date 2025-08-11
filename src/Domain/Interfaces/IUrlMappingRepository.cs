using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Domain.Entities;

namespace Domain.Interfaces
{
    public interface IUrlMappingRepository : IRepository<UrlMapping>
    {
        Task<IEnumerable<UrlMapping>> GetMostClickedAsync(int limit);
        Task<IEnumerable<UrlMapping>> GetActiveAsync();


    }
}