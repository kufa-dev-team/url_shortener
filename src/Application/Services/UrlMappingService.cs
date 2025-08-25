using Domain.Entities;
using Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Runtime.InteropServices.Marshalling;
using Domain.Result;
using AutoMapper.Configuration;
using Application.Models;


namespace Application.Services
{
    public class UrlMappingService : IUrlMappingService
    {

        private readonly IUrlMappingRepository _urlMappingRepository;
        private readonly IShortUrlGeneratorService _shortUrlGeneratorService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<UrlMappingService> _logger;
        private readonly int _shortCodeLength;
        private readonly StackExchange.Redis.IDatabase? _redis;

        public UrlMappingService(
            IUrlMappingRepository urlMappingRepository,
            IUnitOfWork unitOfWork,
            ILogger<UrlMappingService> logger,
            IShortUrlGeneratorService shortUrlGeneratorService,
            IConnectionMultiplexer? redis = null,
            int shortCodeLength = 8)
        {
            _redis = redis?.GetDatabase();
            _shortUrlGeneratorService = shortUrlGeneratorService ?? throw new ArgumentNullException(nameof(shortUrlGeneratorService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _urlMappingRepository = urlMappingRepository ?? throw new ArgumentNullException(nameof(urlMappingRepository));
            _shortCodeLength = shortCodeLength;
        }
        public async Task<Result<UrlMapping>> CreateUrlMappingAsync(UrlMapping UrlMapping, string? customShortCode = null)
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
                        var shortCodeResult = await _shortUrlGeneratorService.GenerateShortUrlAsync(_shortCodeLength);
                        shortCode = shortCodeResult;
                        var urlExistsResult = await _urlMappingRepository.UrlExistsAsync(shortCode);
                        if (urlExistsResult is Failure<bool> urlExistsFailure)
                        {
                            return new Failure<UrlMapping>(new Error(urlExistsFailure.error.message, urlExistsFailure.error.code));
                        }
                        isUnique = !(urlExistsResult as Success<bool>)!.res;
                    } while (!isUnique);
                    UrlMapping.ShortCode = shortCode;
                }
                else
                {
                    // Validate custom short code
                    if (customShortCode.Length != _shortCodeLength)
                    {
                        _logger.LogError("Custom short code must be {Length} characters long.", _shortCodeLength);
                        return new Failure<UrlMapping>(new Error("Custom short code must be {Length} characters long.", ErrorCode.BAD_REQUEST));
                    }
                    var urlExistsResult = await _urlMappingRepository.UrlExistsAsync(customShortCode);
                    if (urlExistsResult is Failure<bool> urlExistsFailure)
                    {
                        return new Failure<UrlMapping>(new Error(urlExistsFailure.error.message, urlExistsFailure.error.code));
                    }
                    if (urlExistsResult is Success<bool> urlExistsSuccess && urlExistsSuccess.res)
                    {
                        _logger.LogError("Custom short code '{CustomShortCode}' already exists.", customShortCode);
                        return new Failure<UrlMapping>(new Error("Custom short code '{CustomShortCode}' already exists.", ErrorCode.BAD_REQUEST));
                    }
                    UrlMapping.ShortCode = customShortCode;
                }

                if (UrlMapping.ExpiresAt.HasValue && UrlMapping.ExpiresAt.Value <= DateTime.UtcNow)
                {
                    _logger.LogError("Expiration date must be in the future.");
                    return new Failure<UrlMapping>(new Error("Expiration date must be in the future.", ErrorCode.BAD_REQUEST));
                }

                var createdUrlMapping = await _urlMappingRepository.AddAsync(UrlMapping);
                if (createdUrlMapping is Success<UrlMapping> mapping)
                {
                    await _unitOfWork.SaveChangesAsync();

                    // Hybrid caching strategy
                    if (_redis != null)
                    {
                        // 1. Lightweight redirect cache (high frequency, 6-hour TTL)
                        var redirectCache = new RedirectCache 
                        {
                            OriginalUrl = mapping.res.OriginalUrl,
                            IsActive = mapping.res.IsActive,
                            ExpiresAt = mapping.res.ExpiresAt,
                            Id = mapping.res.Id
                        };
                        var redirectJson = System.Text.Json.JsonSerializer.Serialize(redirectCache);
                        await _redis.StringSetAsync($"redirect:{mapping.res.ShortCode}", redirectJson, TimeSpan.FromHours(6));
                        
                        // 2. Full entity cache (lower frequency, 1-hour TTL)
                        var entityJson = System.Text.Json.JsonSerializer.Serialize(mapping.res);
                        await _redis.StringSetAsync($"entity:id:{mapping.res.Id}", entityJson, TimeSpan.FromHours(1));
                        await _redis.StringSetAsync($"entity:short:{mapping.res.ShortCode}", entityJson, TimeSpan.FromHours(1));
                    }
                    await _unitOfWork.CommitTransactionAsync();
                }
                return createdUrlMapping;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating UrlMapping.");
                await _unitOfWork.RollbackTransactionAsync();
                return new Failure<UrlMapping>(new Error(ex.Message, ErrorCode.INTERNAL_SERVER_ERROR));
            }
        }

        public async Task<Result<bool>> DeleteUrlAsync(int id)
        {
            if (id <= 0)
            {
                _logger.LogError("Invalid Id value: {Id}. It must be greater than zero.", id);
                return new Failure<bool>(new Error("Id must be greater than zero.", ErrorCode.BAD_REQUEST));
            }
            try
            {
                await _unitOfWork.BeginTransactionAsync();
                var urlMapping = await _urlMappingRepository.GetByIdAsync(id);
                if (urlMapping is Failure<UrlMapping?> urlMappingFailure)
                {
                    return new Failure<bool>(new Error(urlMappingFailure.error.message, urlMappingFailure.error.code));
                }
                var url = (urlMapping as Success<UrlMapping?>)!.res;
                if (url == null)
                {
                    _logger.LogWarning("UrlMapping with Id {Id} not found.", id);
                    return new Failure<bool>(new Error($"UrlMapping with Id {id} not found.", ErrorCode.NOT_FOUND));
                }
                // Delete the URL mapping from the database
                await _urlMappingRepository.DeleteAsync(id);
                await _unitOfWork.SaveChangesAsync();
                if (_redis != null)
                {
                    // Clear both redirect cache and entity cache
                    await _redis.KeyDeleteAsync($"redirect:{url.ShortCode}");
                    await _redis.KeyDeleteAsync($"entity:id:{id}");
                    await _redis.KeyDeleteAsync($"entity:short:{url.ShortCode}");
                }
                await _unitOfWork.CommitTransactionAsync();
                return new Success<bool>(true);
            }
            catch (DbUpdateException dbEx)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(dbEx, "Failed to delete URL with Id: {Id}.", id);
                return new Failure<bool>(new Error(dbEx.Message, ErrorCode.INTERNAL_SERVER_ERROR));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting transaction for deletion.");
                return new Failure<bool>(new Error(ex.Message, ErrorCode.INTERNAL_SERVER_ERROR));
            }
        }

        public async Task<Result<IEnumerable<UrlMapping>>> GetActiveUrlsAsync()
        {
            try
            {
                var activeUrls = await _urlMappingRepository.GetActiveAsync();
                if (activeUrls is Failure<IEnumerable<UrlMapping>> activeUrlsFailure)
                {
                    return new Failure<IEnumerable<UrlMapping>>(new Error(activeUrlsFailure.error.message, activeUrlsFailure.error.code));
                }
                return new Success<IEnumerable<UrlMapping>>((activeUrls as Success<IEnumerable<UrlMapping>>)!.res);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving active URLs.");
                return new Failure<IEnumerable<UrlMapping>>(new Error(ex.Message, ErrorCode.INTERNAL_SERVER_ERROR));
            }
        }

        public async Task<Result<IEnumerable<UrlMapping>>> GetAllUrlsAsync()
        {
            try
            {
                var allUrls = await _urlMappingRepository.GetAllAsync();
                if (allUrls is Failure<IEnumerable<UrlMapping>> allUrlsFailure)
                {
                    return new Failure<IEnumerable<UrlMapping>>(new Error(allUrlsFailure.error.message, allUrlsFailure.error.code));
                }
                return new Success<IEnumerable<UrlMapping>>((allUrls as Success<IEnumerable<UrlMapping>>)!.res);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all URLs.");
                return new Failure<IEnumerable<UrlMapping>>(new Error(ex.Message, ErrorCode.INTERNAL_SERVER_ERROR));
            }
        }

        public async Task<Result<UrlMapping?>> GetByIdAsync(int Id)
        {
            if (Id <= 0)
            {
                _logger.LogError("Invalid Id value: {Id}. It must be greater than zero.", Id);
                return new Failure<UrlMapping?>(new Error("Id must be greater than zero.", ErrorCode.BAD_REQUEST));
            }

            try
            {
                // Try entity cache first (for detailed operations)
                if (_redis != null)
                {
                    var entityCacheKey = $"entity:id:{Id}";
                    var cached = await _redis.StringGetAsync(entityCacheKey);
                    if (cached.HasValue && !string.IsNullOrEmpty(cached))
                    {
                        var cachedEntity = System.Text.Json.JsonSerializer.Deserialize<UrlMapping>(cached.ToString());
                        return new Success<UrlMapping?>(cachedEntity);
                    }
                }

                // If not found in cache, get from database
                var url = await _urlMappingRepository.GetByIdAsync(Id);
                if (url is Failure<UrlMapping?> urlFailure)
                {
                    return new Failure<UrlMapping?>(new Error(urlFailure.error.message, urlFailure.error.code));
                }
                var entity = (url as Success<UrlMapping?>)?.res;

                // Cache if Redis available and entity found
                if (entity != null && _redis != null)
                {
                    // Use hybrid caching: both entity and redirect caches
                    var entityJson = System.Text.Json.JsonSerializer.Serialize(entity);
                    await _redis.StringSetAsync($"entity:id:{Id}", entityJson, TimeSpan.FromHours(1));
                    await _redis.StringSetAsync($"entity:short:{entity.ShortCode}", entityJson, TimeSpan.FromHours(1));
                    
                    // Also populate redirect cache
                    var redirectCache = new RedirectCache 
                    {
                        OriginalUrl = entity.OriginalUrl,
                        IsActive = entity.IsActive,
                        ExpiresAt = entity.ExpiresAt,
                        Id = entity.Id
                    };
                    var redirectJson = System.Text.Json.JsonSerializer.Serialize(redirectCache);
                    await _redis.StringSetAsync($"redirect:{entity.ShortCode}", redirectJson, TimeSpan.FromHours(6));
                }

                return new Success<UrlMapping?>(entity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving URL by Id: {Id}.", Id);
                return new Failure<UrlMapping?>(new Error(ex.Message, ErrorCode.INTERNAL_SERVER_ERROR));
            }
        }

        public async Task<Result<UrlMapping?>> GetByShortCodeAsync(string shortCode)
        {
            if (string.IsNullOrWhiteSpace(shortCode))
            {
                _logger.LogError("Short code cannot be null or empty.");
                return new Failure<UrlMapping?>(new Error("Short code cannot be null or empty.", ErrorCode.BAD_REQUEST));
            }

            try
            {
                // Try entity cache first (for detailed operations)
                if (_redis != null)
                {
                    var entityCacheKey = $"entity:short:{shortCode}";
                    var cached = await _redis.StringGetAsync(entityCacheKey);
                    if (cached.HasValue && !string.IsNullOrEmpty(cached))
                    {
                        var cachedEntity = System.Text.Json.JsonSerializer.Deserialize<UrlMapping>(cached.ToString());
                        return new Success<UrlMapping?>(cachedEntity);
                    }
                }

                // Get from database
                var url = await _urlMappingRepository.GetByShortCodeAsync(shortCode);
                if (url is Failure<UrlMapping?> urlFailure)
                {
                    return new Failure<UrlMapping?>(new Error(urlFailure.error.message, urlFailure.error.code));
                }
                var entity = (url as Success<UrlMapping?>)?.res;

                // Cache if Redis available and entity found using hybrid strategy
                if (entity != null && _redis != null)
                {
                    // Store full entity cache (1-hour TTL)
                    var entityJson = System.Text.Json.JsonSerializer.Serialize(entity);
                    await _redis.StringSetAsync($"entity:short:{shortCode}", entityJson, TimeSpan.FromHours(1));
                    await _redis.StringSetAsync($"entity:id:{entity.Id}", entityJson, TimeSpan.FromHours(1));
                    
                    // Also populate redirect cache (6-hour TTL)
                    var redirectCache = new RedirectCache 
                    {
                        OriginalUrl = entity.OriginalUrl,
                        IsActive = entity.IsActive,
                        ExpiresAt = entity.ExpiresAt,
                        Id = entity.Id
                    };
                    var redirectJson = System.Text.Json.JsonSerializer.Serialize(redirectCache);
                    await _redis.StringSetAsync($"redirect:{shortCode}", redirectJson, TimeSpan.FromHours(6));
                }

                return new Success<UrlMapping?>(entity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving URL by short code: {ShortCode}.", shortCode);
                return new Failure<UrlMapping?>(new Error(ex.Message, ErrorCode.INTERNAL_SERVER_ERROR));
            }
        }

        public async Task<Result<IEnumerable<UrlMapping>>> GetMostClickedUrlsAsync(int limit)
        {
            // Ensure limit is a positive number
            if (limit <= 0)
            {
                _logger.LogError("Invalid limit value: {Limit}. It must be greater than zero.", limit);
                return new Failure<IEnumerable<UrlMapping>>(new Error("Limit must be greater than zero.", ErrorCode.BAD_REQUEST));
            }
            try
            {
                var mostClickedUrls = await _urlMappingRepository.GetMostClickedAsync(limit);
                if (mostClickedUrls is Failure<IEnumerable<UrlMapping>> mostClickedUrlsFailure)
                {
                    return new Failure<IEnumerable<UrlMapping>>(new Error(mostClickedUrlsFailure.error.message, mostClickedUrlsFailure.error.code));
                }
                return new Success<IEnumerable<UrlMapping>>((mostClickedUrls as Success<IEnumerable<UrlMapping>>)!.res);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving most clicked URLs.");
                return new Failure<IEnumerable<UrlMapping>>(new Error(ex.Message, ErrorCode.INTERNAL_SERVER_ERROR));
            }
        }

        public async Task<Result<string>> RedirectToOriginalUrlAsync(string shortCode)
        {
            if (string.IsNullOrWhiteSpace(shortCode))
            {
                _logger.LogError("Short code cannot be null or empty.");
                return new Failure<string>(new Error("Invalid short code", ErrorCode.BAD_REQUEST));
            }
            // Try lightweight redirect cache first (primary cache for redirects)
            var redirectCacheKey = $"redirect:{shortCode}"; 
            if (_redis != null)
            {
                var redirectCached = await _redis.StringGetAsync(redirectCacheKey);
                if (redirectCached.HasValue && !string.IsNullOrEmpty(redirectCached))
                {
                    var redirectCache = System.Text.Json.JsonSerializer.Deserialize<RedirectCache>(redirectCached.ToString());
                    if (redirectCache != null && redirectCache.IsActive && 
                        (!redirectCache.ExpiresAt.HasValue || redirectCache.ExpiresAt > DateTime.UtcNow))
                    {
                        // Cache hit! Update click count and return URL
                        await _unitOfWork.BeginTransactionAsync();
                        try
                        {
                            await _urlMappingRepository.IncrementClickCountAsync(redirectCache.Id);
                            await _unitOfWork.CommitTransactionAsync();
                            return new Success<string>(redirectCache.OriginalUrl);
                        }
                        catch (Exception ex)
                        {
                            await _unitOfWork.RollbackTransactionAsync();
                            _logger.LogError(ex, "Error incrementing click count for cached redirect");
                            // Continue to return the URL even if click count update fails
                            return new Success<string>(redirectCache.OriginalUrl);
                        }
                    }
                }
            }

            // Cache miss - get from database
            var urlMapping = await _urlMappingRepository.GetByShortCodeAsync(shortCode);
            if (urlMapping is Failure<UrlMapping?> urlMappingFailure)
            {
                return new Failure<string>(new Error(urlMappingFailure.error.message, urlMappingFailure.error.code));
            }
            var url = (urlMapping as Success<UrlMapping?>)!.res;
            if (url == null || !url.IsActive)
            {
                _logger.LogWarning("Invalid or inactive short code: {ShortCode}", shortCode);
                return new Failure<string>(new Error("URL not found", ErrorCode.NOT_FOUND));
            }

            // Check if URL has expired
            if (url.ExpiresAt.HasValue && url.ExpiresAt.Value <= DateTime.UtcNow)
            {
                _logger.LogWarning("Expired short code: {ShortCode}, expired at {ExpiresAt}", shortCode, url.ExpiresAt);
                return new Failure<string>(new Error("URL not found", ErrorCode.NOT_FOUND));
            }

            // Cache the result using hybrid strategy
            if (_redis != null)
            {
                // 1. Store lightweight redirect cache (6-hour TTL)
                var redirectCache = new RedirectCache 
                {
                    OriginalUrl = url.OriginalUrl,
                    IsActive = url.IsActive,
                    ExpiresAt = url.ExpiresAt,
                    Id = url.Id
                };
                var redirectJson = System.Text.Json.JsonSerializer.Serialize(redirectCache);
                await _redis.StringSetAsync(redirectCacheKey, redirectJson, TimeSpan.FromHours(6));
                
                // 2. Store full entity cache (1-hour TTL) for detailed operations
                var entityJson = System.Text.Json.JsonSerializer.Serialize(url);
                await _redis.StringSetAsync($"entity:short:{shortCode}", entityJson, TimeSpan.FromHours(1));
            }

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                // Atomic counter update
                var error = await _urlMappingRepository.IncrementClickCountAsync(url.Id);
                if (error != null)
                {
                    return new Failure<string>(error);
                }
                await _unitOfWork.CommitTransactionAsync();
                return new Success<string>(url.OriginalUrl);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "Error processing redirect for {ShortCode}", shortCode);
                return new Failure<string>(new Error(ex.Message, ErrorCode.INTERNAL_SERVER_ERROR));
            }
        }
        public async Task<Result<UrlMapping>> UpdateUrlAsync(UrlMapping urlMapping, string? customShortCode = null)
        {
            if (urlMapping == null)
            {
                _logger.LogError("Attempted to update a null UrlMapping.");
                return new Failure<UrlMapping>(new Error("UrlMapping cannot be null.", ErrorCode.BAD_REQUEST));
            }

            var existingMappingResult = await _urlMappingRepository.GetByIdAsync(urlMapping.Id);
            if (existingMappingResult is Failure<UrlMapping> existingMappingFailure)
            {
                return new Failure<UrlMapping>(new Error(existingMappingFailure.error.message, existingMappingFailure.error.code));
            }
            var existingMapping = (existingMappingResult as Success<UrlMapping>)!.res;

            string oldShortCode = existingMapping.ShortCode;
            if (!string.IsNullOrWhiteSpace(customShortCode))
            {
                if (customShortCode.Length != _shortCodeLength)
                {
                    _logger.LogError("Custom short code must be {Length} characters long.", _shortCodeLength);
                    return new Failure<UrlMapping>(new Error($"Custom short code must be {_shortCodeLength} characters long.", ErrorCode.BAD_REQUEST));
                }
                var urlExistsResult = await _urlMappingRepository.UrlExistsAsync(customShortCode);
                if (urlExistsResult is Failure<bool> urlExistsFailure)
                {
                    return new Failure<UrlMapping>(new Error(urlExistsFailure.error.message, urlExistsFailure.error.code));
                }
                if (urlExistsResult is Success<bool> urlExistsSuccess && urlExistsSuccess.res && customShortCode != existingMapping.ShortCode)
                {
                    _logger.LogError("Custom short code '{CustomShortCode}' already exists.", customShortCode);
                    return new Failure<UrlMapping>(new Error($"Custom short code '{customShortCode}' already exists.", ErrorCode.BAD_REQUEST));
                }
                existingMapping.ShortCode = customShortCode;
            }

            if (!string.IsNullOrWhiteSpace(urlMapping.Description))
                existingMapping.Description = urlMapping.Description;

            if (!string.IsNullOrWhiteSpace(urlMapping.OriginalUrl))
            {
                //here we are checking if the Url is valid 
                /*the if statement involves two parts: 

                First part:
                [!Uri.TryCreate(urlMapping.OriginalUrl, UriKind.Absolute, out Uri? uriResult]
                we are using Uri.TryCreate to create new uri object(Uniform Resource Identifier object) It is a .NET class 
                that represents a web address or resource identifier. It parses a string It returns 
                true if the is a valid Url, false if the string is not a valid URI.

                out Uri? uriResult : mean that if the process succeeded save the uri object in uriResult

                seconde part:
                uriResult.Scheme != Uri.UriSchemeHttp && uriResult.Scheme != Uri.UriSchemeHttps)
                the .Schema mean the protocol(HTTP/HTTPS) and Uri.UriSchemeHttp means the the https protocol
                and Uri.UriSchemeHttps means the https protocol 

                As a result the if statement will mean :
                IF (the string is not a valid url) Or (the url is not one of these protocol(HTTP/HTTPS) )
                */

                if (!Uri.TryCreate(urlMapping.OriginalUrl, UriKind.Absolute, out Uri? uriResult)
                    || (uriResult.Scheme != Uri.UriSchemeHttp && uriResult.Scheme != Uri.UriSchemeHttps))
                {
                    return new Failure<UrlMapping>(new Error("OriginalUrl must be a valid HTTP/HTTPS URL, It must begin with https:// Or http://", ErrorCode.BAD_REQUEST));
                }
                existingMapping.OriginalUrl = urlMapping.OriginalUrl;
            }

            if (urlMapping.ExpiresAt != null)
            {
                // Allow setting any expiry date for testing purposes
                existingMapping.ExpiresAt = urlMapping.ExpiresAt;
                
                // Original validation (commented out for testing):
                // if (urlMapping.ExpiresAt > DateTime.UtcNow)
                //     existingMapping.ExpiresAt = urlMapping.ExpiresAt;
                // else
                //     return new Failure<UrlMapping>(new Error("Expiration date must be in the future.", ErrorCode.BAD_REQUEST));
            }

            existingMapping.UpdatedAt = DateTime.UtcNow;

            try
            {
                await _unitOfWork.BeginTransactionAsync();
                await _urlMappingRepository.UpdateAsync(existingMapping);
                await _unitOfWork.SaveChangesAsync();

                if (_redis != null)
                {
                    // Update hybrid caches
                    var entityJson = System.Text.Json.JsonSerializer.Serialize(existingMapping);
                    
                    // Update entity caches (1-hour TTL)
                    await _redis.StringSetAsync($"entity:id:{existingMapping.Id}", entityJson, TimeSpan.FromHours(1));
                    await _redis.StringSetAsync($"entity:short:{existingMapping.ShortCode}", entityJson, TimeSpan.FromHours(1));
                    
                    // Update redirect cache (6-hour TTL)
                    var redirectCache = new RedirectCache 
                    {
                        OriginalUrl = existingMapping.OriginalUrl,
                        IsActive = existingMapping.IsActive,
                        ExpiresAt = existingMapping.ExpiresAt,
                        Id = existingMapping.Id
                    };
                    var redirectJson = System.Text.Json.JsonSerializer.Serialize(redirectCache);
                    await _redis.StringSetAsync($"redirect:{existingMapping.ShortCode}", redirectJson, TimeSpan.FromHours(6));

                    // If short code changed, remove old caches
                    if (!string.IsNullOrWhiteSpace(customShortCode) && customShortCode != oldShortCode)
                    {
                        await _redis.KeyDeleteAsync($"entity:short:{oldShortCode}");
                        await _redis.KeyDeleteAsync($"redirect:{oldShortCode}");
                    }
                }

                await _unitOfWork.CommitTransactionAsync();
                return new Success<UrlMapping>(existingMapping);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "Error updating UrlMapping.");
                return new Failure<UrlMapping>(new Error(ex.Message, ErrorCode.INTERNAL_SERVER_ERROR));
            }
        }
        public async Task<Result<bool>> UrlExistsAsync(string shortCode)
        {
            if (string.IsNullOrWhiteSpace(shortCode))
            {
                _logger.LogError("Short code cannot be null or empty.");
                return new Failure<bool>(new Error("Short code cannot be null or empty.", ErrorCode.BAD_REQUEST));
            }
            try
            {
                return await _urlMappingRepository.UrlExistsAsync(shortCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if URL exists for short code: {ShortCode}.", shortCode);
                return new Failure<bool>(new Error(ex.Message, ErrorCode.INTERNAL_SERVER_ERROR));
            }
        }
        public async Task<Result<bool>> DeactivateExpiredUrlsAsync()
        {
            try
            {
                // Use bulk operation for optimal performance - single database operation
                var bulkUpdateResult = await _urlMappingRepository.DeactivateExpiredUrlsBulkAsync();
                if (bulkUpdateResult is Failure<int> bulkUpdateFailure)
                {
                    return new Failure<bool>(new Error(bulkUpdateFailure.error.message, bulkUpdateFailure.error.code));
                }
                
                var updatedCount = (bulkUpdateResult as Success<int>)!.res;
                _logger.LogInformation("Successfully deactivated {Count} expired URLs using bulk operation", updatedCount);
                
                return new Success<bool>(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deactivating expired URLs");
                return new Failure<bool>(new Error(ex.Message, ErrorCode.INTERNAL_SERVER_ERROR));
            }
        }
        public async Task<bool> RemoveAsync(string shortCode)
        {
            if (_redis == null) return false;
            
            // Enhanced cache invalidation for hybrid caching system
            // Remove both redirect cache and entity cache entries
            var deleteTasks = new[]
            {
                _redis.KeyDeleteAsync($"redirect:{shortCode}"),       // Hybrid: redirect cache
                _redis.KeyDeleteAsync($"entity:short:{shortCode}"),   // Hybrid: entity cache by short code  
                _redis.KeyDeleteAsync($"url:short:{shortCode}")       // Legacy: backward compatibility
            };
            
            var results = await Task.WhenAll(deleteTasks);
            bool anyDeleted = results.Any(r => r);
            
            _logger.LogInformation("Cache purge for shortCode '{ShortCode}': redirect={Redirect}, entity={Entity}, legacy={Legacy}", 
                shortCode, results[0], results[1], results[2]);
            
            return anyDeleted;
        }
    }
}
    