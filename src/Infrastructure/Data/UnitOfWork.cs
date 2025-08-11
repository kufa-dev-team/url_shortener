using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.Marshalling;
using System.Threading.Tasks;
using Domain.Interfaces;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore.Storage;

namespace Infrastructure.Data
{
    //we created the UnitOfWork in the data layer because it is part of the data access logic, just like your DbContext 
    //Best practice of UnitOfWork is to primarily use it in your business logic layer — that means in your services or controllers, so we shouldn't use it inside repositories themselves.

    public class UnitOfWork : IUnitOfWork
{
    private IDbContextTransaction _currentTransaction;
    private readonly ApplicationDbContext _context;
    
    // Repository property
    public IUrlMappingRepository UrlMappings { get; }
    
    // Transaction state properties
    public bool HasActiveTransaction => _currentTransaction != null;
    public bool isActiveTransaction => HasActiveTransaction; // Now properly implemented

    public UnitOfWork(ApplicationDbContext context, IUrlMappingRepository urlMappingRepository)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        UrlMappings = urlMappingRepository ?? throw new ArgumentNullException(nameof(urlMappingRepository));
    }

    // Explicit interface implementation for cleanup
    Task IUnitOfWork.CleanupTransactionAsync() => CleanupTransactionAsync();

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (HasActiveTransaction)
            throw new InvalidOperationException("A transaction is already active");

        _currentTransaction = await _context.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (!HasActiveTransaction)
            throw new InvalidOperationException("No active transaction to commit");

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            await _currentTransaction.CommitAsync(cancellationToken);
        }
        finally
        {
            await CleanupTransactionAsync();
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (!HasActiveTransaction) return;

        try
        {
            await _currentTransaction.RollbackAsync(cancellationToken);
        }
        finally
        {
            await CleanupTransactionAsync();
        }
    }

    private async Task CleanupTransactionAsync()
    {
        if (_currentTransaction != null)
        {
            await _currentTransaction.DisposeAsync();
            _currentTransaction = null;
        }
    }

    public async ValueTask DisposeAsync()
    {
        await CleanupTransactionAsync();
        await _context.DisposeAsync();
    }

    public void Dispose()
    {
        _currentTransaction?.Dispose();
        _context.Dispose();
    }

    }
}