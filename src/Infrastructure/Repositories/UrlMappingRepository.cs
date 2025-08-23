
using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Domain.Result;

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
        public async Task<Result<UrlMapping>> AddAsync(UrlMapping entity)
        {
            if (entity == null)
            {
                _logger.LogError("Attempted to add a null UrlMapping entity");
                throw new ArgumentNullException(nameof(entity), "UrlMapping entity cannot be null");
            }
            
            try {
                // Add the UrlMapping entity to the DbSet and return the added entity
                var AddedUrl = await _dbSet.AddAsync(entity);
                return new Success<UrlMapping>(AddedUrl.Entity);
            } catch (Exception ex) {
                _logger.LogError(ex, "Error adding UrlMapping");
                return new Failure<UrlMapping>(new Error("Error adding UrlMapping", ErrorCode.INTERNAL_SERVER_ERROR));
            }
        }

        public async Task<Error?> DeleteAsync(int Id)
        {
            try {

                var urlMapping = await _dbSet.FirstOrDefaultAsync(u => u.Id == Id);
                if (urlMapping == null)
                {
                    _logger.LogWarning($"No UrlMapping found for Id: {Id}");
                    return new Error($"No UrlMapping found for Id: {Id}", ErrorCode.NOT_FOUND);
                }
                //if it not null, remove it from the DbSet
                _dbSet.Remove(urlMapping);
                return null;
            } catch (Exception ex) {
                _logger.LogError(ex, "Error deleting UrlMapping");
                return new Error(ex.Message, ErrorCode.INTERNAL_SERVER_ERROR);
            }
        }

        public Task UpdateAsync(UrlMapping urlMapping)
        {
            if (urlMapping == null)
            {
                _logger.LogError("Attempted to update a null UrlMapping entity");
                throw new ArgumentNullException(nameof(urlMapping), "UrlMapping entity cannot be null");
            }
            
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
        public async Task<Result<IEnumerable<UrlMapping>>> GetActiveAsync()
        {
            try {
                // Fetch all active URL and return the activ as a list
                var activeurls = await _dbSet
                .Where(u => u.IsActive == true)
                .ToListAsync();

                return new Success<IEnumerable<UrlMapping>>(activeurls);
            } catch (Exception ex) {
                _logger.LogError(ex, "Error fetching active URLs");
                return new Failure<IEnumerable<UrlMapping>>(new Error(ex.Message, ErrorCode.INTERNAL_SERVER_ERROR));
            }
        }

        public async Task<Result<IEnumerable<UrlMapping>>> GetAllAsync()
        {
            try {
                var allurls = await _dbSet.ToListAsync();
                return new Success<IEnumerable<UrlMapping>>(allurls);
            } catch (Exception ex) {
                _logger.LogError(ex, "Error fetching all URLs");
                return new Failure<IEnumerable<UrlMapping>>(new Error(ex.Message, ErrorCode.INTERNAL_SERVER_ERROR));
            }
        }

        public async Task<Result<UrlMapping?>> GetByIdAsync(int Id)
        {

            try {
                var url = await _dbSet.Where(u => u.Id == Id)
                .FirstOrDefaultAsync();
                return new Success<UrlMapping?>(url);
            } catch (Exception ex) {
                _logger.LogError(ex, "Error fetching URL by Id");
                return new Failure<UrlMapping?>(new Error(ex.Message, ErrorCode.INTERNAL_SERVER_ERROR));
            }
        }


        public async Task<Result<IEnumerable<UrlMapping>>> GetMostClickedAsync(int limit)
        {
            if (limit <= 0)
            {
                _logger.LogError("Invalid limit value: {Limit}. It must be greater than zero.", limit);
                return new Failure<IEnumerable<UrlMapping>>(new Error("Limit must be greater than zero.", ErrorCode.BAD_REQUEST));
            }
            // Fetch the most clicked URLs, ordered by ClickCount in descending order
            // and limited to the specified number of results
            try {
                var mostClickedUrls = await _dbSet
                .Where(u => u.ClickCount > 0)
                .OrderByDescending(u => u.ClickCount)
                .Take(limit)
                .AsNoTracking() // Better performance for read-only
                .ToListAsync();
                return new Success<IEnumerable<UrlMapping>>(mostClickedUrls);
            } catch (Exception ex) {
                _logger.LogError(ex, "Error fetching most clicked URLs");
                return new Failure<IEnumerable<UrlMapping>>(new Error(ex.Message, ErrorCode.INTERNAL_SERVER_ERROR));
            }
        }
        public async Task<Result<UrlMapping?>> GetByShortCodeAsync(string shortCode)
        {
            if (shortCode == null)
            {
                _logger.LogError("Short code cannot be null");
                throw new ArgumentNullException(nameof(shortCode), "Short code cannot be null");
            }
            
            if (string.IsNullOrEmpty(shortCode))
            {
                _logger.LogError("Short code cannot be empty");
                throw new ArgumentException("Short code cannot be empty", nameof(shortCode));
            }
            
            try {
                var url = await _dbSet.Where(u => u.ShortCode == shortCode)
                .FirstOrDefaultAsync();
                return new Success<UrlMapping?>(url);
            } catch (Exception ex) {
                _logger.LogError(ex, "Error fetching URL by short code");
                return new Failure<UrlMapping?>(new Error(ex.Message, ErrorCode.INTERNAL_SERVER_ERROR));
            }
        }
        public async Task<Result<bool>> UrlExistsAsync(string shortCode)
        {
            try {
                var exists = await _dbSet.AnyAsync(u => u.ShortCode == shortCode);
                return new Success<bool>(exists);
            } catch (Exception ex) {
                _logger.LogError(ex, "Error checking if URL exists");
                return new Failure<bool>(new Error(ex.Message, ErrorCode.INTERNAL_SERVER_ERROR));
            }
        }

        public async Task<Error?> IncrementClickCountAsync(int id)
        {
            try {
                await _context.UrlMappings
                    .Where(u => u.Id == id)
                    .ExecuteUpdateAsync(u =>
                        u.SetProperty(x => x.ClickCount, x => x.ClickCount + 1));
                return null;
            } catch (Exception ex) {
                _logger.LogError(ex, "Error incrementing click count");
                return new Error(ex.Message, ErrorCode.INTERNAL_SERVER_ERROR);
            }
        }

        public async Task<Result<IEnumerable<UrlMapping>>> GetExpiredUrlsAsync()
        {
            try {
                var expiredUrls = await _context.UrlMappings
                .Where(u => u.IsActive && u.ExpiresAt < DateTime.UtcNow)
                .ToListAsync();
                return new Success<IEnumerable<UrlMapping>>(expiredUrls);
            } catch (Exception ex) {
                _logger.LogError(ex, "Error fetching expired URLs");
                return new Failure<IEnumerable<UrlMapping>>(new Error(ex.Message, ErrorCode.INTERNAL_SERVER_ERROR));
            }
        }

    }
}
