using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Domain.Interfaces
{
    public interface IUnitOfWork
    {
        // Define the repositories that are part of the unit of work
        IUrlMappingRepository UrlMappingRepository { get; }
        
        //save changes to the database
        //save changes will return the number of affected rows or entities
        Task<int> SaveChangesAsync();
        // Dispose method to clean up resources
        void Dispose();

    }
}