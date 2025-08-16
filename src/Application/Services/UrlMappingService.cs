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
        public async Task<UrlMapping> CreateUrlMappingAsync(UrlMapping UrlMapping, string? customShortCode = null)
        {
            if (UrlMapping == null)
            {
                _logger.LogError("Attempted to create a null UrlMapping.");
                throw new ArgumentNullException(nameof(UrlMapping), "UrlMapping cannot be null.");
            }
            UrlMapping.CreatedAt = DateTime.UtcNow;
            UrlMapping.UpdatedAt = DateTime.UtcNow;
            UrlMapping.IsActive = true;
            UrlMapping.ClickCount = 0;
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                if (string.IsNullOrWhiteSpace(customShortCode))
                {
                    string shortCode;
                    bool isUnique;
                    do
                    {
                        shortCode = await _shortUrlGeneratorService.GenerateShortUrlAsync(_shortCodeLength);
                        isUnique = !await _urlMappingRepository.UrlExistsAsync(shortCode);
                    } while (!isUnique);
                    UrlMapping.ShortCode = shortCode;
                }
                else
                {
                    // Validate custom short code
                    if (customShortCode.Length != _shortCodeLength)
                    {
                        _logger.LogError("Custom short code must be {Length} characters long.", _shortCodeLength);
                        throw new ArgumentException($"Custom short code must be {_shortCodeLength} characters long.", nameof(customShortCode));
                    }
                    if (await _urlMappingRepository.UrlExistsAsync(customShortCode))
                    {
                        _logger.LogError("Custom short code '{CustomShortCode}' already exists.", customShortCode);
                        throw new InvalidOperationException($"Custom short code '{customShortCode}' already exists.");
                    }
                    UrlMapping.ShortCode = customShortCode;
                }


                if (UrlMapping.ExpiresAt.HasValue && UrlMapping.ExpiresAt.Value <= DateTime.UtcNow)
                {
                    _logger.LogError("Expiration date must be in the future.");
                    throw new ArgumentException("Expiration date must be in the future.", nameof(UrlMapping.ExpiresAt));
                }
                var createdUrlMapping = await _urlMappingRepository.AddAsync(UrlMapping);
                await _unitOfWork.SaveChangesAsync();

                await _redis.StringSetAsync($"url:{createdUrlMapping.ShortCode}", UrlMapping.OriginalUrl, TimeSpan.FromDays(30));
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

            if (Id < 0)
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
                throw new ArgumentException("Invalid short code", nameof(shortCode));
            }

            var urlMapping = await _urlMappingRepository.GetByShortCodeAsync(shortCode);
            if (urlMapping == null || !urlMapping.IsActive)
            {
                _logger.LogWarning("Invalid or inactive short code: {ShortCode}", shortCode);
                throw new KeyNotFoundException("URL not found");
            }
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                // Atomic counter update
                await _urlMappingRepository.IncrementClickCountAsync(urlMapping.Id);
                await _unitOfWork.CommitTransactionAsync();
                return urlMapping.OriginalUrl;
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "Error processing redirect for {ShortCode}", shortCode);
                throw; // Re-throw for controller handling
            }
        }

        public async Task UpdateUrlAsync(UrlMapping urlMapping, string? customShortCode = null)
        {
            if (urlMapping == null)
            {
                _logger.LogError("Attempted to update a null UrlMapping.");
                throw new ArgumentNullException(nameof(urlMapping), "UrlMapping cannot be null.");
            }
            var existingMapping = await _urlMappingRepository.GetByIdAsync(urlMapping.Id) ?? throw new KeyNotFoundException("UrlMapping not found.");
            existingMapping.Title = urlMapping.Title;
            existingMapping.Description = urlMapping.Description;
            existingMapping.OriginalUrl = urlMapping.OriginalUrl;
            existingMapping.ExpiresAt = urlMapping.ExpiresAt;
            existingMapping.IsActive = urlMapping.IsActive;
            existingMapping.UpdatedAt = DateTime.UtcNow;
            try
            {
                await _unitOfWork.BeginTransactionAsync();
                await _urlMappingRepository.UpdateAsync(existingMapping);
                if (!string.IsNullOrWhiteSpace(customShortCode))
                {
                    if (customShortCode.Length != _shortCodeLength)
                    {
                        _logger.LogError("Custom short code must be {Length} characters long.", _shortCodeLength);
                        throw new ArgumentException($"Custom short code must be {_shortCodeLength} characters long.", nameof(customShortCode));
                    }
                    if (await _urlMappingRepository.UrlExistsAsync(customShortCode) && customShortCode != existingMapping.ShortCode)
                    {
                        _logger.LogError("Custom short code '{CustomShortCode}' already exists.", customShortCode);
                        throw new InvalidOperationException($"Custom short code '{customShortCode}' already exists.");
                    }
                    existingMapping.ShortCode = customShortCode;
                }
                await _unitOfWork.SaveChangesAsync();

                var redisKey = $"url:{existingMapping.ShortCode}";
                await _redis.StringSetAsync(
                    redisKey,
                    existingMapping.OriginalUrl.Trim(),
                    TimeSpan.FromDays(30));

                // Only delete old key if short code changed
                if (!string.IsNullOrWhiteSpace(customShortCode) &&
                    existingMapping.ShortCode != urlMapping.ShortCode)
                {
                    await _redis.KeyDeleteAsync($"url:{urlMapping.ShortCode}");
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
        public async Task DeactivateExpiredUrlsAsync()
        {
            try
            {
                // Get all URLs that are active but expired
                var expiredUrls = await _urlMappingRepository.GetExpiredUrlsAsync();

                foreach (var url in expiredUrls)
                {
                    url.IsActive = false;
                    await _urlMappingRepository.UpdateAsync(url);
                }

                await _unitOfWork.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deactivating expired URLs");
            }
        }
    }
}
    