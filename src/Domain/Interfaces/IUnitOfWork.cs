using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Domain.Interfaces
{
    public interface IUnitOfWork : IDisposable, IAsyncDisposable
    {
        // Define the repositories that are part of the unit of work
        IUrlMappingRepository UrlMappings { get; }

        //save changes to the database
        //save changes will return the number of affected rows or entities
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

        Task BeginTransactionAsync(CancellationToken cancellationToken = default);
        Task CommitTransactionAsync(CancellationToken cancellationToken = default);
        Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
        Task CleanupTransactionAsync();

        bool isActiveTransaction { get; }
        
    }
}