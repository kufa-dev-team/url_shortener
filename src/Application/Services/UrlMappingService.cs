using Domain.Entities;
using Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Runtime.InteropServices.Marshalling;
using Domain.Result;
using AutoMapper.Configuration;


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
                        if (urlExistsResult is Failure<bool> urlExistsFailure) {
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
                    if (urlExistsResult is Failure<bool> urlExistsFailure) {
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
                if (createdUrlMapping is Success<UrlMapping> mapping) {
                    await _unitOfWork.SaveChangesAsync();

                    // Cache the full entity in Redis by id and by short code
                    if (_redis != null)
                    {
                        var entityJson = System.Text.Json.JsonSerializer.Serialize(mapping.res);
                        await _redis.StringSetAsync($"url:id:{mapping.res.Id}", entityJson, TimeSpan.FromDays(1));
                        await _redis.StringSetAsync($"url:short:{mapping.res.ShortCode}", entityJson, TimeSpan.FromDays(1));
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

        public async Task<Error?> DeleteUrlAsync(int id)
        {
            if (id <= 0)
            {
                _logger.LogError("Invalid Id value: {Id}. It must be greater than zero.", id);
                return new Error("Id must be greater than zero.", ErrorCode.BAD_REQUEST);
            }
            try
            {
                await _unitOfWork.BeginTransactionAsync();
                var urlMapping = await _urlMappingRepository.GetByIdAsync(id);
                if (urlMapping is Failure<UrlMapping> urlMappingFailure) {
                    return new Error(urlMappingFailure.error.message, urlMappingFailure.error.code);
                }
                var url = (urlMapping as Success<UrlMapping>)!.res;
                if (url == null)
                {
                    _logger.LogWarning("UrlMapping with Id {Id} not found.", id);
                    return new Error($"UrlMapping with Id {id} not found.", ErrorCode.NOT_FOUND);
                }
                // Delete the URL mapping from the database
                await _urlMappingRepository.DeleteAsync(id);
                await _unitOfWork.SaveChangesAsync();
                if (_redis != null)
                {
                    await _redis.KeyDeleteAsync($"url:short:{url.ShortCode}");//remove from Redis only if then the delete operation is successful in the database
                    await _redis.KeyDeleteAsync($"url:id:{id}");// remove the cache entry for the URL by Id
                }
                await _unitOfWork.CommitTransactionAsync();
                return null;
            }
            catch (DbUpdateException dbEx)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(dbEx, "Failed to delete URL with Id: {Id}.", id);
                return new Error(dbEx.Message, ErrorCode.INTERNAL_SERVER_ERROR);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting transaction for deletion.");
                return new Error(ex.Message, ErrorCode.INTERNAL_SERVER_ERROR);
            }
        }

        public async Task<Result<IEnumerable<UrlMapping>>> GetActiveUrlsAsync()
        {
            try
            {
                var activeUrls = await _urlMappingRepository.GetActiveAsync();
                if (activeUrls is Failure<IEnumerable<UrlMapping>> activeUrlsFailure) {
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
                if (allUrls is Failure<IEnumerable<UrlMapping>> allUrlsFailure) {
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

            if (Id < 0)
            {
                _logger.LogError("Invalid Id value: {Id}. It must be greater than zero.", Id);
                return new Failure<UrlMapping?>(new Error("Id must be greater than zero.", ErrorCode.BAD_REQUEST));
            }
            try
            {
                // Try Redis cache first (if available)
                if (_redis != null)
                {
                    var cacheKey = $"url:id:{Id}";
                    var cached = await _redis.StringGetAsync(cacheKey);
                    if (cached.HasValue)
                    {
                        var cachedEntity = System.Text.Json.JsonSerializer.Deserialize<UrlMapping>(cached);
                        return new Success<UrlMapping?>(cachedEntity);
                    }
                }
               
                // If not found in cache, get from database
                var url = await _urlMappingRepository.GetByIdAsync(Id);
                if (url is Failure<UrlMapping> urlFailure) {
                    return new Failure<UrlMapping?>(new Error(urlFailure.error.message, urlFailure.error.code));
                }
                var entity = (url as Success<UrlMapping>)?.res;

                // Cache if Redis available and entity found
                if (entity != null && _redis != null)
                {
                    var cacheKey = $"url:id:{Id}";
                    await _redis.StringSetAsync(cacheKey, System.Text.Json.JsonSerializer.Serialize(entity), TimeSpan.FromDays(1));
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
                // Try Redis cache first (if available)
                if (_redis != null)
                {
                    var cacheKey = $"url:short:{shortCode}";
                    var cached = await _redis.StringGetAsync(cacheKey);
                    if (cached.HasValue)
                    {
                        var cachedEntity = System.Text.Json.JsonSerializer.Deserialize<UrlMapping>(cached);
                        return new Success<UrlMapping?>(cachedEntity);
                    }
                }

                // Get from database
                var url = await _urlMappingRepository.GetByShortCodeAsync(shortCode);
                if (url is Failure<UrlMapping> urlFailure) {
                    return new Failure<UrlMapping?>(new Error(urlFailure.error.message, urlFailure.error.code));
                }
                var entity = (url as Success<UrlMapping>)?.res;

                // Cache if Redis available and entity found
                if (entity != null && _redis != null)
                {
                    var cacheKey2 = $"url:short:{shortCode}";
                    await _redis.StringSetAsync(cacheKey2, System.Text.Json.JsonSerializer.Serialize(entity), TimeSpan.FromDays(1));
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
                if (mostClickedUrls is Failure<IEnumerable<UrlMapping>> mostClickedUrlsFailure) {
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
            var cacheKey = $"url:short:{shortCode}";
            if (_redis != null)
            {
                var cached = await _redis.StringGetAsync(cacheKey);
                if (cached.HasValue)
                {
                    // Deserialize and return only the OriginalUrl
                    var entity = System.Text.Json.JsonSerializer.Deserialize<UrlMapping>(cached);
                    if (entity != null && entity.IsActive)
                    {
                        return new Success<string>(entity.OriginalUrl);
                    }
                }
            }

            // If not found in cache, get from database
            var urlMapping = await _urlMappingRepository.GetByShortCodeAsync(shortCode);
            if (urlMapping is Failure<UrlMapping> urlMappingFailure) {
                return new Failure<string>(new Error(urlMappingFailure.error.message, urlMappingFailure.error.code));
            }
            var url = (urlMapping as Success<UrlMapping>)!.res;
            if (url == null || !url.IsActive)
            {
                _logger.LogWarning("Invalid or inactive short code: {ShortCode}", shortCode);
                return new Failure<string>(new Error("URL not found", ErrorCode.NOT_FOUND));
            }

            // Store the full entity in cache for next time (TTL 1 day)
            if (_redis != null)
            {
                var entityJson = System.Text.Json.JsonSerializer.Serialize(url);
                await _redis.StringSetAsync(cacheKey, entityJson, TimeSpan.FromDays(1));
            }

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                // Atomic counter update
                var error = await _urlMappingRepository.IncrementClickCountAsync(url.Id);
                if (error != null) {
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
            if (existingMappingResult is Failure<UrlMapping> existingMappingFailure) {
                return new Failure<UrlMapping>(new Error(existingMappingFailure.error.message, existingMappingFailure.error.code));
            }
            var existingMapping = (existingMappingResult as Success<UrlMapping>)!.res;
            
            if (!string.IsNullOrWhiteSpace(customShortCode))
            {
                if (customShortCode.Length != _shortCodeLength)
                {
                    _logger.LogError("Custom short code must be {Length} characters long.", _shortCodeLength);
                    return new Failure<UrlMapping>(new Error($"Custom short code must be {_shortCodeLength} characters long.", ErrorCode.BAD_REQUEST));
                }
                var urlExistsResult = await _urlMappingRepository.UrlExistsAsync(customShortCode);
                if (urlExistsResult is Failure<bool> urlExistsFailure) {
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
                if (urlMapping.ExpiresAt > DateTime.UtcNow)
                    existingMapping.ExpiresAt = urlMapping.ExpiresAt;
                else
                    return new Failure<UrlMapping>(new Error("Expiration date must be in the future.", ErrorCode.BAD_REQUEST));
            }
            
            existingMapping.UpdatedAt = DateTime.UtcNow;

            try
            {
                await _unitOfWork.BeginTransactionAsync();
                await _urlMappingRepository.UpdateAsync(existingMapping);
                await _unitOfWork.SaveChangesAsync();

                if (_redis != null)
                {
                    var redisKey = $"url:{existingMapping.ShortCode}";
                    await _redis.StringSetAsync(
                        redisKey,
                        existingMapping.OriginalUrl.Trim(),
                        TimeSpan.FromDays(1));

                    // Only delete old key if short code changed
                    if (!string.IsNullOrWhiteSpace(customShortCode) &&
                        customShortCode != existingMapping.ShortCode)
                    {
                        await _redis.KeyDeleteAsync($"url:{existingMapping.ShortCode}");
                    }
                }

                await _unitOfWork.CommitTransactionAsync();
                return new Success<UrlMapping>(existingMapping);  // Return the updated entity
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
        public async Task<Error?> DeactivateExpiredUrlsAsync()
        {
            try
            {
                // Get all URLs that are active but expired
                var expiredUrls = await _urlMappingRepository.GetExpiredUrlsAsync();
                if (expiredUrls is Failure<IEnumerable<UrlMapping>> expiredUrlsFailure) {
                    return new Error(expiredUrlsFailure.error.message, expiredUrlsFailure.error.code);
                }
                var urls = (expiredUrls as Success<IEnumerable<UrlMapping>>)!.res;
                foreach (var url in urls)
                {
                    url.IsActive = false;
                    await _urlMappingRepository.UpdateAsync(url);
                }

                await _unitOfWork.SaveChangesAsync();
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deactivating expired URLs");
                return new Error(ex.Message, ErrorCode.INTERNAL_SERVER_ERROR);
            }
        }
    }
}
    