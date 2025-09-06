using API.DTOs.UrlMapping;
using Microsoft.AspNetCore.Mvc;
using Domain.Entities;
using Domain.Interfaces;
using Domain.Result;
using NewRelic.Api.Agent; // New Relic APM API for custom metrics & traces
using System.Collections.Generic; // For custom events payloads

namespace API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class UrlShortenerController : BaseController
    {
        private readonly ILogger<UrlShortenerController> _logger;
        private readonly IUrlMappingService _urlMappingService;

        public UrlShortenerController(
            ILogger<UrlShortenerController> logger,
            IUrlMappingService urlMappingService)
        {
            _urlMappingService = urlMappingService;
            _logger = logger;
        }

        [HttpPost]
        [ProducesResponseType(typeof(CreateUrlMappingResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [Trace] // Enable detailed tracing for this action
        public async Task<ActionResult<CreateUrlMappingResponse>> CreateShortUrl([FromBody] CreateUrlMappingRequest request)
        {
            // Start timing for latency measurement
            var sw = System.Diagnostics.Stopwatch.StartNew();

            // Name the transaction clearly in APM (category, name)
            NewRelic.Api.Agent.NewRelic.SetTransactionName("UrlShortener", "Create");

            // Acquire agent and current transaction to add attributes
            var agent = NewRelic.Api.Agent.NewRelic.GetAgent();
            var tx = agent.CurrentTransaction;

            // Add useful custom attributes for filtering in APM
            tx.AddCustomAttribute("endpoint", "POST /UrlShortener");
            tx.AddCustomAttribute("hasCustomShortCode", !string.IsNullOrWhiteSpace(request.CustomShortCode));

            try
            {
                var urlMapping = new UrlMapping
                {
                    OriginalUrl = request.OriginalUrl,
                    ExpiresAt = request.ExpiresAt,
                    Title = request.Title,
                    Description = request.Description,
                };
                var CreatedUrl = await _urlMappingService.CreateUrlMappingAsync(urlMapping, request.CustomShortCode);
                if (CreatedUrl is Failure<UrlMapping> failure)
                {
                    // Mark as failure and record a custom event
                    tx.AddCustomAttribute("success", false);
                    tx.AddCustomAttribute("errorCode", (int)failure.error.code);
                    NewRelic.Api.Agent.NewRelic.RecordCustomEvent("UrlCreate", new Dictionary<string, object>
                    {
                        {"outcome", "Failure"},
                        {"errorCode", (int)failure.error.code}
                    });
                    return StatusCode((int)failure.error.code, failure.error.message);
                }
                if (CreatedUrl is Success<UrlMapping> url)
                {
                    // Mark as success and record a custom event
                    tx.AddCustomAttribute("success", true);
                    tx.AddCustomAttribute("shortCode", url.res.ShortCode ?? string.Empty);
                    NewRelic.Api.Agent.NewRelic.RecordCustomEvent("UrlCreate", new Dictionary<string, object>
                    {
                        {"outcome", "Success"},
                        {"shortCode", url.res.ShortCode ?? string.Empty}
                    });

                    return CreatedAtAction
                    (
                        nameof(GetUrlById),
                        new { id = url.res.Id },
                        new CreateUrlMappingResponse
                        {
                            Id = url.res.Id,
                            ShortCode = url.res.ShortCode,
                            OriginalUrl = url.res.OriginalUrl,
                            ShortUrl = $"{Request.Scheme}://{Request.Host}/{url.res.ShortCode}",
                            CreatedAt = url.res.CreatedAt,
                            UpdatedAt = url.res.UpdatedAt,
                            Title = url.res.Title,
                            Description = url.res.Description,
                            ExpiresAt = url.res.ExpiresAt,
                            IsActive = url.res.IsActive,
                            ClickCount = url.res.ClickCount
                        }
                    );
                }
                // Unexpected path
                tx.AddCustomAttribute("success", false);
                NewRelic.Api.Agent.NewRelic.RecordCustomEvent("UrlCreate", new Dictionary<string, object>
                {
                    {"outcome", "Unexpected"}
                });
                return StatusCode(500, "Internal server error");
            }
            finally
            {
                // Stop timer and submit latency metric
                sw.Stop();
                tx.AddCustomAttribute("latencyMs", sw.ElapsedMilliseconds);
                NewRelic.Api.Agent.NewRelic.RecordCustomEvent("UrlCreateLatency", new Dictionary<string, object>
                {
                    {"latencyMs", sw.ElapsedMilliseconds}
                });
            }
        }

        [HttpDelete]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteUrlMapping(int id)
        {
            if (id <= 0)
            {
                return BadRequest("The Id must be larger that Zero ");
            }

            var urlMappingResult = await _urlMappingService.GetByIdAsync(id);
            if (urlMappingResult is Failure<UrlMapping?> urlMappingFailure)
            {
                return StatusCode((int)urlMappingFailure.error.code, urlMappingFailure.error.message);
            }
            var urlMapping = (urlMappingResult as Success<UrlMapping?>)?.res;
            if (urlMapping == null)
            {
                return NotFound($"No URL mapping found for Id: {id}");
            }

            var deleteResult = await _urlMappingService.DeleteUrlAsync(id);
            if (deleteResult is Failure<bool> deleteFailure)
            {
                return StatusCode((int)deleteFailure.error.code, deleteFailure.error.message);
            }
            return NoContent();
        }
        [HttpPut]
        public async Task<ActionResult<UrlMappingResponse>> UpdateUrl([FromBody] UpdateUrlMappingRequest request)
        {
            var existingUrl = await _urlMappingService.GetByIdAsync(request.Id);
            if (existingUrl is Failure<UrlMapping> existingUrlFailure)
            {
                return StatusCode((int)existingUrlFailure.error.code, existingUrlFailure.error.message);
            }
            var url = (existingUrl as Success<UrlMapping>)?.res;
            if (url == null)
            {
                return NotFound($"URL with ID {request.Id} not found.");
            }
            url.Title = request.Title ?? url.Title;
            url.Description = request.Description ?? url.Description;
            url.OriginalUrl = request.OriginalUrl ?? url.OriginalUrl;
            url.ExpiresAt = request.ExpiresAt;
            url.IsActive = request.IsActive;
            url.UpdatedAt = DateTime.UtcNow;

            var updateUrlResult = await _urlMappingService.UpdateUrlAsync(url, request.CustomShortCode);
            if (updateUrlResult is Failure<UrlMapping> updateUrlFailure)
            {
                return StatusCode((int)updateUrlFailure.error.code, updateUrlFailure.error.message);
            }
            var updatedUrl = (updateUrlResult as Success<UrlMapping>)?.res;
            if (updatedUrl == null)
            {
                return StatusCode(500, "Internal server error");
            }
            return Ok(new UrlMappingResponse
            {
                Id = updatedUrl.Id,
                ShortCode = updatedUrl.ShortCode,
                OriginalUrl = updatedUrl.OriginalUrl,
                ShortUrl = $"{Request.Scheme}://{Request.Host}/{updatedUrl.ShortCode}",
                Title = updatedUrl.Title,
                Description = updatedUrl.Description,
                ExpiresAt = updatedUrl.ExpiresAt,
                IsActive = updatedUrl.IsActive,
                ClickCount = updatedUrl.ClickCount
            });

        }
        [HttpGet("GetAll")]
        public async Task<ActionResult<IEnumerable<UrlMappingResponse>>> GetAllUrls()
        {
            var urlMappingsResult = await _urlMappingService.GetAllUrlsAsync();
            if (urlMappingsResult is Failure<IEnumerable<UrlMapping>> urlMappingsFailure)
            {
                return StatusCode((int)urlMappingsFailure.error.code, urlMappingsFailure.error.message);
            }
            var urlMappings = (urlMappingsResult as Success<IEnumerable<UrlMapping>>)!.res;
            return Ok(urlMappings.Select(um => new UrlMappingResponse
            {
                Id = um.Id,
                OriginalUrl = um.OriginalUrl,
                ShortCode = um.ShortCode,
                ShortUrl = $"{Request.Scheme}://{Request.Host}/{um.ShortCode}",
                CreatedAt = um.CreatedAt,
                ClickCount = um.ClickCount,
                ExpiresAt = um.ExpiresAt,
                IsActive = um.IsActive,
                Title = um.Title,
                Description = um.Description
            }));
        }
        [HttpGet("GetById/{id}")]
        public async Task<ActionResult<UrlMappingResponse>> GetUrlById(int id)
        {
            var existingUrlResult = await _urlMappingService.GetByIdAsync(id);
            if (existingUrlResult is Failure<UrlMapping> existingUrlFailure)
            {
                return StatusCode((int)existingUrlFailure.error.code, existingUrlFailure.error.message);
            }
            var url = (existingUrlResult as Success<UrlMapping>)?.res;
            if (url == null)
            {
                return NotFound($"URL with ID {id} not found.");
            }
            return Ok(new UrlMappingResponse
            {
                Id = url.Id,
                ShortCode = url.ShortCode,
                OriginalUrl = url.OriginalUrl,
                ShortUrl = $"{Request.Scheme}://{Request.Host}/{url.ShortCode}",
                Title = url.Title,
                Description = url.Description,
                ExpiresAt = url.ExpiresAt,
                IsActive = url.IsActive,
                ClickCount = url.ClickCount
            });
        }
        [HttpGet("MostClicked/{limit}")]
        public async Task<ActionResult<IEnumerable<UrlMappingResponse>>> GetMostClickedUrl(int limit)
        {
            if (limit <= 0 || limit > 1000)
            {
                return BadRequest("Limit must be between 1 and 1000");
            }
            var popularUrlsResult = await _urlMappingService.GetMostClickedUrlsAsync(limit);
            if (popularUrlsResult is Failure<IEnumerable<UrlMapping>> popularUrlsFailure)
            {
                return StatusCode((int)popularUrlsFailure.error.code, popularUrlsFailure.error.message);
            }
            var popularUrls = (popularUrlsResult as Success<IEnumerable<UrlMapping>>)!.res;
            return Ok(popularUrls.Select(um => new UrlMappingResponse
            {
                Id = um.Id,
                OriginalUrl = um.OriginalUrl,
                ShortUrl = $"{Request.Scheme}://{Request.Host}/{um.ShortCode}",
                ShortCode = um.ShortCode,
                Title = um.Title,
                Description = um.Description,
                ExpiresAt = um.ExpiresAt,
                ClickCount = um.ClickCount,
                IsActive = um.IsActive,
                CreatedAt = um.CreatedAt
            }));
        }

        [HttpGet("/allActiveUrls")]
        public async Task<ActionResult<IEnumerable<UrlMappingResponse>>> GetActiveUrls()
        {
            var activeUrlsResult = await _urlMappingService.GetActiveUrlsAsync();
            if (activeUrlsResult is Failure<IEnumerable<UrlMapping>> activeUrlsFailure)
            {
                return StatusCode((int)activeUrlsFailure.error.code, activeUrlsFailure.error.message);
            }
            var activeUrls = (activeUrlsResult as Success<IEnumerable<UrlMapping>>)!.res;
            return Ok(activeUrls.Select(um => new UrlMappingResponse
            {
                Id = um.Id,
                OriginalUrl = um.OriginalUrl,
                ShortUrl = $"{Request.Scheme}://{Request.Host}/{um.ShortCode}",
                ShortCode = um.ShortCode,
                Title = um.Title,
                Description = um.Description,
                ExpiresAt = um.ExpiresAt,
                ClickCount = um.ClickCount,
                IsActive = um.IsActive,
                CreatedAt = um.CreatedAt
            }));
        }
        // Redirect endpoint: GET /{shortCode}
        // This endpoint returns a 302 redirect to the original URL if found, or 404 if not found.
        [HttpGet("/{shortCode}")]
        [ProducesResponseType(StatusCodes.Status302Found)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Trace] // Enable detailed tracing for redirect
        public async Task<IActionResult> RedirectByShortCode(string shortCode)
        {
            // Start timing and name the transaction for APM
            var sw = System.Diagnostics.Stopwatch.StartNew();
            NewRelic.Api.Agent.NewRelic.SetTransactionName("UrlShortener", "Redirect");
            var agent = NewRelic.Api.Agent.NewRelic.GetAgent();
            var tx = agent.CurrentTransaction;
            tx.AddCustomAttribute("endpoint", "GET /{shortCode}");
            tx.AddCustomAttribute("shortCode", shortCode);

            _logger.LogTrace("Redirect requested for short code: {ShortCode}", shortCode);

            try
            {
                var originalUrlResult = await _urlMappingService.RedirectToOriginalUrlAsync(shortCode);
                if (originalUrlResult is Failure<string> originalUrlFailure)
                {
                    // Failure path: record and return
                    tx.AddCustomAttribute("success", false);
                    tx.AddCustomAttribute("errorCode", (int)originalUrlFailure.error.code);
                    NewRelic.Api.Agent.NewRelic.RecordCustomEvent("Redirect", new Dictionary<string, object>
                    {
                        {"outcome", "Failure"},
                        {"errorCode", (int)originalUrlFailure.error.code},
                        {"shortCode", shortCode}
                    });

                    _logger.LogWarning("Short code not found: {ShortCode}", shortCode);
                    return StatusCode((int)originalUrlFailure.error.code, originalUrlFailure.error.message);
                }
                var originalUrl = (originalUrlResult as Success<string>)?.res;

                if (string.IsNullOrEmpty(originalUrl))
                {
                    tx.AddCustomAttribute("success", false);
                    NewRelic.Api.Agent.NewRelic.RecordCustomEvent("Redirect", new Dictionary<string, object>
                    {
                        {"outcome", "NotFound"},
                        {"shortCode", shortCode}
                    });

                    _logger.LogWarning("Short code not found: {ShortCode}", shortCode);
                    return NotFound("Short URL not found");
                }

                // Success path
                tx.AddCustomAttribute("success", true);
                NewRelic.Api.Agent.NewRelic.RecordCustomEvent("Redirect", new Dictionary<string, object>
                {
                    {"outcome", "Success"},
                    {"shortCode", shortCode}
                });

                _logger.LogInformation("Redirecting short code {ShortCode} to {OriginalUrl}", shortCode, originalUrl);
                return Redirect(originalUrl);
            }
            finally
            {
                // Always emit latency metric
                sw.Stop();
                tx.AddCustomAttribute("latencyMs", sw.ElapsedMilliseconds);
                NewRelic.Api.Agent.NewRelic.RecordCustomEvent("RedirectLatency", new Dictionary<string, object>
                {
                    {"latencyMs", sw.ElapsedMilliseconds},
                    {"shortCode", shortCode}
                });
            }
        }

        [HttpPost("DeactivateExpired")]
        public async Task<IActionResult> DeactivateExpired()
        {
            var deactivateExpiredResult = await _urlMappingService.DeactivateExpiredUrlsAsync();
            if (deactivateExpiredResult is Failure<bool> deactivateFailure)
            {
                return StatusCode((int)deactivateFailure.error.code, deactivateFailure.error.message);
            }
            return NoContent();
        }
        
        [HttpDelete("admin/cache/{shortCode}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> PurgeByCode(string shortCode)
        {
            var deleted = await _urlMappingService.RemoveAsync(shortCode);
            if (!deleted)
                return NotFound($"ShortCode '{shortCode}' not found in cache.");

            return NoContent(); // 204


        }

        
    }
}
