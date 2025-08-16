
using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Repositories
{
    public class UrlMappingRepository : IUrlMappingRepository, IRepository<UrlMapping>
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
        public async Task<UrlMapping> AddAsync(UrlMapping entity)
        {

            // Add the UrlMapping entity to the DbSet and return the added entity
            var AddedUrl = await _dbSet.AddAsync(entity);
            return AddedUrl.Entity;
        }

        public async Task DeleteAsync(int Id)
        {
            var urlMapping = await _dbSet.FirstOrDefaultAsync(u => u.Id == Id);
            if (urlMapping == null)
            {
                _logger.LogWarning($"No UrlMapping found for Id: {Id}");
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

        public async Task<UrlMapping?> GetByIdAsync(int Id)
        {

            return await _dbSet.Where(u => u.Id == Id)
                .FirstOrDefaultAsync();
        }


        public async Task<IEnumerable<UrlMapping>> GetMostClickedAsync(int limit)
        {
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
        public async Task<UrlMapping?> GetByShortCodeAsync(string shortCode)
        {


            return await _dbSet
                .Where(u => u.ShortCode == shortCode)
                .FirstOrDefaultAsync();
        }
        public async Task<bool> UrlExistsAsync(string shortCode)
        {
            // Check if a URL mapping with the given short code exists
            return await _dbSet.AnyAsync(u => u.ShortCode == shortCode);
        }

        public async Task IncrementClickCountAsync(int id)
        {
            await _context.UrlMappings
                .Where(u => u.Id == id)
                .ExecuteUpdateAsync(u =>
                    u.SetProperty(x => x.ClickCount, x => x.ClickCount + 1));
        }

        public async Task<IEnumerable<UrlMapping>> GetExpiredUrlsAsync()
        {
            return await _context.UrlMappings
                .Where(u => u.IsActive && u.ExpiresAt < DateTime.UtcNow)
                .ToListAsync();
        }

    }
}
