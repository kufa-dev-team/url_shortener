using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Repositories
{
    public class UrlMappingRepository : IUrlMappingRepository
    {
        // The DbContext instance for database operations
        // The DbSet for UrlMapping entities
        // The logger for logging operations and errors
        private readonly ApplicationDbContext _context;
        private readonly DbSet<UrlMapping> _dbSet;
        private readonly ILogger<UrlMappingRepository> _logger;


        // Constructor to initialize the repository with the DbContext and logger
        public UrlMappingRepository(ApplicationDbContext context, ILogger<UrlMappingRepository> logger)
        {
            this._context = context;
            this._dbSet = context.Set<UrlMapping>();
            _logger = logger;
        }

        // Implement the methods defined in the IUrlMappingRepository interface
        public async Task<UrlMapping> AddAsync(UrlMapping urlMapping)
        {
            // Check if urlMapping is null to avoid adding null entries
            if (urlMapping == null)
            {
                _logger.LogError("Attempted to add a null UrlMapping.");
                throw new ArgumentNullException(nameof(urlMapping), "UrlMapping cannot be null.");
            }
            // Add the UrlMapping entity to the DbSet and return the added entity
            await _dbSet.AddAsync(urlMapping);
            return urlMapping;
        }

        public async Task DeleteAsync(string ShortCode)
        {
            var urlMapping = await _dbSet.FirstOrDefaultAsync(u => u.OriginalUrl == ShortCode);
            if (urlMapping == null)
            {
                _logger.LogWarning($"No UrlMapping found for LongUrl: {ShortCode}");
                return;
            }
            //if it not null, remove it from the DbSet
            _dbSet.Remove(urlMapping);
            return;
        }

        public Task UpdateAsync(UrlMapping urlMapping)
        {
            /*
            the method is not anync because we are not using any async operations inside it
            we are just updating the entity in the DbSet
            we are using the Entry method to attach the entity to the context
            and mark it as modified
            */
               
            if (urlMapping == null)
            {
                _logger.LogError("Attempted to update a null UrlMapping.");
                throw new ArgumentNullException(nameof(urlMapping), "UrlMapping cannot be null.");
            }
            /* 
            Attach the entity to the context and mark it as modified
            we just marks the whole entity as modified, 
            so when we call _context.SaveChanges(),
            EF will generate an UPDATE statement for all columns of this entity in the database.
            */
            _context.Entry(urlMapping).State = EntityState.Modified;
            return Task.CompletedTask;
        }
        public async Task<IEnumerable<UrlMapping>> GetActiveAsync()
        {
            // Fetch all active URL and return the activ as a list
            var activeurls = await _dbSet
            .Where(u => u.IsActive == true)
            .ToListAsync();

            return activeurls;
        }

        public async Task<IEnumerable<UrlMapping>> GetAllAsync()
        {
            return await _dbSet.ToListAsync();
        }

        public async Task<UrlMapping?> GetByShortUrlAsync(string ShortCode)
        {
            if (string.IsNullOrWhiteSpace(ShortCode))
            {
                _logger.LogError("ShortCode cannot be null or empty.");
                throw new ArgumentException("ShortCode cannot be null or empty.", nameof(ShortCode));
            }
            return await _dbSet.Where(u => u.ShortCode == ShortCode)
                .FirstOrDefaultAsync();
        }

        public async Task<UrlMapping?> GetByTitleAsync(string Title)
        {
            return await _dbSet
                .FirstOrDefaultAsync(u => u.Title != null &&
                    u.Title.Equals(Title, StringComparison.OrdinalIgnoreCase));

        }

        public async Task<IEnumerable<UrlMapping>> GetMostClickedAsync(int limit)
        {
            // Ensure limit is a positive number
            if (limit <= 0)
            {
                _logger.LogError("Invalid limit value: {Limit}. It must be greater than zero.", limit);
                throw new ArgumentOutOfRangeException(nameof(limit), "Limit must be greater than zero.");
            }
            // Fetch the most clicked URLs, ordered by ClickCount in descending order
            // and limited to the specified number of results
            return await _dbSet
                .Where(u => u.ClickCount > 0)
                .OrderByDescending(u => u.ClickCount)
                .Take(limit)
                .AsNoTracking() // Better performance for read-only
                .ToListAsync();
        }

        
    }
}