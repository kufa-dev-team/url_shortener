using Domain.Entities;
using Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

namespace Application.Services
{
    public class UrlMappingService : IUrlMappingService
    {

        private readonly IUrlMappingRepository _urlMappingRepository;
        private readonly IShortUrlGeneratorService _shortUrlGeneratorService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<UrlMappingService> _logger;
        private readonly int _shortCodeLength;
        private readonly StackExchange.Redis.IDatabase _redis;

        public UrlMappingService(
            IUrlMappingRepository urlMappingRepository,
            IUnitOfWork unitOfWork,
            ILogger<UrlMappingService> logger,
            IShortUrlGeneratorService shortUrlGeneratorService,
            IConnectionMultiplexer redis,
            int shortCodeLength = 8)
        {
            _redis = (redis ?? throw new ArgumentNullException(nameof(redis))).GetDatabase();
            _shortUrlGeneratorService = shortUrlGeneratorService ?? throw new ArgumentNullException(nameof(shortUrlGeneratorService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _urlMappingRepository = urlMappingRepository ?? throw new ArgumentNullException(nameof(urlMappingRepository));
            _shortCodeLength = shortCodeLength;
        }
        public async Task<UrlMapping> CreateUrlMappingAsync(UrlMapping UrlMapping)
        {
            if (UrlMapping == null)
            {
                _logger.LogError("Attempted to create a null UrlMapping.");
                throw new ArgumentNullException(nameof(UrlMapping), "UrlMapping cannot be null.");
            }
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                string shortCode;
                bool isUnique;
                do
                {
                    shortCode = await _shortUrlGeneratorService.GenerateShortUrlAsync(_shortCodeLength);
                    isUnique = !await _urlMappingRepository.UrlExistsAsync(shortCode);
                } while (!isUnique);
                var urlMapping = new UrlMapping
                {
                    OriginalUrl = UrlMapping.OriginalUrl.Trim(), // Ensure no leading/trailing spaces
                    ShortCode = shortCode,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    ExpiresAt = UrlMapping.ExpiresAt,
                    Title = UrlMapping.Title,
                    Description = UrlMapping.Description,
                    ClickCount = 0,
                    IsActive = true
                };

                if (urlMapping.ExpiresAt.HasValue && urlMapping.ExpiresAt.Value <= DateTime.UtcNow)
                {
                    _logger.LogError("Expiration date must be in the future.");
                    throw new ArgumentException("Expiration date must be in the future.", nameof(urlMapping.ExpiresAt));
                }
                var createdUrlMapping = await _urlMappingRepository.AddAsync(urlMapping);
                await _unitOfWork.SaveChangesAsync();
                
                await _redis.StringSetAsync($"url:{shortCode}", urlMapping.OriginalUrl, TimeSpan.FromDays(30));
                await _unitOfWork.CommitTransactionAsync();
                return createdUrlMapping;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating UrlMapping.");
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task DeleteUrlAsync(int id)
        {
            if (id <= 0)
            {
                _logger.LogError("Invalid Id value: {Id}. It must be greater than zero.", id);
                throw new ArgumentOutOfRangeException(nameof(id), "Id must be greater than zero.");
            }
            try
            {
                await _unitOfWork.BeginTransactionAsync();
                var urlMapping = await _urlMappingRepository.GetByIdAsync(id);
            if (urlMapping == null)
            {
                _logger.LogWarning("UrlMapping with Id {Id} not found.", id);
                throw new KeyNotFoundException($"UrlMapping with Id {id} not found.");
            }
                // Remove the URL mapping from Redis cache
                
                // Delete the URL mapping from the database
                await _urlMappingRepository.DeleteAsync(id);
                await _unitOfWork.SaveChangesAsync();
                await _redis.KeyDeleteAsync($"url:{urlMapping.ShortCode}");//remove from Redis only if then the delete operation is successful in the database
                await _unitOfWork.CommitTransactionAsync();
            }
            catch (DbUpdateException dbEx)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(dbEx, "Failed to delete URL with Id: {Id}.", id);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting transaction for deletion.");
                throw;
            }
        }

        public async Task<IEnumerable<UrlMapping>> GetActiveUrlsAsync()
        {
            try
            {
                return await _urlMappingRepository.GetActiveAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving active URLs.");
                throw;
            }
        }

        public async Task<IEnumerable<UrlMapping>> GetAllUrlsAsync()
        {
            try
            {
                return await _urlMappingRepository.GetAllAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all URLs.");
                throw;
            }
        }

        public async Task<UrlMapping?> GetByIdAsync(int Id)
        {

            if (Id <= 0)
            {
                _logger.LogError("Invalid Id value: {Id}. It must be greater than zero.", Id);
                throw new ArgumentOutOfRangeException(nameof(Id), "Id must be greater than zero.");
            }
            try
            {
                return await _urlMappingRepository.GetByIdAsync(Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving URL by Id: {Id}.", Id);
                throw;
            }
        }

        public async Task<UrlMapping?> GetByShortCodeAsync(string shortCode)
        {
            if (string.IsNullOrWhiteSpace(shortCode))
            {
                _logger.LogError("Short code cannot be null or empty.");
                throw new ArgumentException("Short code cannot be null or empty.", nameof(shortCode));
            }
            try
            {
                return await _urlMappingRepository.GetByShortCodeAsync(shortCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving URL by short code: {ShortCode}.", shortCode);
                throw;
            }
        }

        public async Task<IEnumerable<UrlMapping>> GetMostClickedUrlsAsync(int limit)
        {
            // Ensure limit is a positive number
            if (limit <= 0)
            {
                _logger.LogError("Invalid limit value: {Limit}. It must be greater than zero.", limit);
                throw new ArgumentOutOfRangeException(nameof(limit), "Limit must be greater than zero.");
            }
            try
            {
                return await _urlMappingRepository.GetMostClickedAsync(limit);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving most clicked URLs.");
                throw;
            }
        }

        public async Task<string> RedirectToOriginalUrlAsync(string shortCode)
        {
            if (string.IsNullOrWhiteSpace(shortCode))
            {
                _logger.LogError("Short code cannot be null or empty.");
                throw new ArgumentException("Short code cannot be null or empty.", nameof(shortCode));
            }
            // Check if the URL is cached in Redis
            string? longUrl = await _redis.StringGetAsync($"url:{shortCode}");

            if (longUrl != null) 
            {
                return longUrl; // Cache hit (no DB query needed)
            }
            try
            {
                string? originalUrl = await _urlMappingRepository.RedirectToOriginalUrlAsync(shortCode);
                await _unitOfWork.SaveChangesAsync(); // Ensure changes are saved after incrementing click count
                await _redis.StringSetAsync($"url:{shortCode}", originalUrl, TimeSpan.FromDays(30));
                return originalUrl ?? throw new KeyNotFoundException("Short code not found.");
            }
            /*
                The (?? throw new KeyNotFoundException) line throws the exception, but does not log it.
                The (catch KeyNotFoundException) block ensures you log the failure (e.g., for monitoring, debugging, or analytics).
                Without logging, we'd have no record of how often invalid short codes are requested.
            */
            catch (KeyNotFoundException knfEx)
            {
                _logger.LogWarning(knfEx, "Short code not found: {ShortCode}.", shortCode);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error redirecting to original URL for short code: {ShortCode}.", shortCode);
                throw;
            }
        }

        public async Task UpdateUrlAsync(UrlMapping urlMapping)
        {
            if (urlMapping == null)
            {
                _logger.LogError("Attempted to update a null UrlMapping.");
                throw new ArgumentNullException(nameof(urlMapping), "UrlMapping cannot be null.");
            }
            var existingMapping = await _urlMappingRepository.GetByIdAsync(urlMapping.Id) ?? throw new KeyNotFoundException("UrlMapping not found.");
            try
            {
                await _unitOfWork.BeginTransactionAsync();
                await _urlMappingRepository.UpdateAsync(urlMapping);
                urlMapping.UpdatedAt = DateTime.UtcNow;
                await _unitOfWork.SaveChangesAsync();
                
                await _redis.StringSetAsync(
                    key: $"url:{existingMapping.ShortCode}",
                    value: urlMapping.OriginalUrl.Trim(), // Ensure no leading/trailing spaces
                    expiry: TimeSpan.FromDays(30)
                );
                //delete the old short code from Redis if it has changed
                if (existingMapping.ShortCode != urlMapping.ShortCode)
                {
                    await _redis.KeyDeleteAsync($"url:{existingMapping.ShortCode}");
                }

                await _unitOfWork.CommitTransactionAsync();
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "Error updating UrlMapping.");
                throw;
            }
        }
        public async Task<bool> UrlExistsAsync(string shortCode)
        {
            if (string.IsNullOrWhiteSpace(shortCode))
            {
                _logger.LogError("Short code cannot be null or empty.");
                throw new ArgumentException("Short code cannot be null or empty.", nameof(shortCode));
            }
            try
            {
                return await _urlMappingRepository.UrlExistsAsync(shortCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if URL exists for short code: {ShortCode}.", shortCode);
                throw;
            }
        }
    }
}
    