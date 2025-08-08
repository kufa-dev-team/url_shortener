using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Domain.Interfaces;
using Infrastructure.Repositories;

namespace Infrastructure.Data
{
    //we created the UnitOfWork in the data layer because it is part of the data access logic, just like your DbContext 
    //Best practice of UnitOfWork is to primarily use it in your business logic layer — that means in your services or controllers, so we shouldn't use it inside repositories themselves.

    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _context;
        public IUrlMappingRepository UrlMappings{ get; }

        public IUrlMappingRepository UrlMappingRepository => throw new NotImplementedException();

        //we use the repository to access the UrlMappingRepository methods
        //this is the repository that will be used to access the UrlMappingRepository methods
        //public IUrlMappingRepository UrlMappingRepository => _urlMappingRepository;

        // Constructor to initialize the UnitOfWork with the DbContext and repositories
        public UnitOfWork(ApplicationDbContext context, IUrlMappingRepository urlMappingRepository)
        {
            _context = context ;
            UrlMappings = urlMappingRepository;
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}