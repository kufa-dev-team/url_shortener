
using API.DTOs.UrlMapping;
using Microsoft.AspNetCore.Mvc;
using Domain.Entities;
using Domain.Interfaces;

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
        public async Task<ActionResult<CreateUrlMappingResponse>> CreateShortUrl([FromBody] CreateUrlMappingRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var urlMapping = new UrlMapping
            {
                OriginalUrl = request.OriginalUrl,
                ExpiresAt = request.ExpiresAt,
                Title = request.Title,
                Description = request.Description
            };
            await _urlMappingService.CreateUrlMappingAsync(urlMapping);
            return CreatedAtAction
            (
                nameof(CreateShortUrl),
                new { id = urlMapping.Id },
                new CreateUrlMappingResponse
                {
                    Id = urlMapping.Id,
                    ShortCode = urlMapping.ShortCode,
                    ExpiresAt = urlMapping.ExpiresAt
                }
            );
        }
        [HttpDelete]
        public async Task<IActionResult> DeleteUrlMapping(int Id)
        {
            if (Id <= 0)
            {
                return BadRequest("The Id must be larger that Zero ");
            }

            var urlMapping = await _urlMappingService.GetByIdAsync(Id);
            if (urlMapping == null)
            {
                return NotFound($"No URL mapping found for Id: {Id}");
            }

            await _urlMappingService.DeleteUrlAsync(Id);
            return NoContent();
        }
        [HttpPut]
        public async Task<ActionResult<UrlMappingResponse>> UpdateUrl([FromBody] UpdateUrlMappingRequest request)
        {
            var existingUrl = await _urlMappingService.GetByIdAsync(request.Id);
            if (existingUrl == null)
            {
                return NotFound($"URL with ID {request.Id} not found.");
            }
            UrlMapping urlMapping = new UrlMapping
            {
                Id = request.Id,
                Title = request.Title,
                Description = request.Description,
                OriginalUrl = request.OriginalUrl,
                ExpiresAt = request.ExpiresAt,
                // Preserve existing important fields
                ShortCode = existingUrl.ShortCode,
                CreatedAt = existingUrl.CreatedAt,
                UpdatedAt = DateTime.UtcNow,
                ClickCount = existingUrl.ClickCount,
                IsActive = existingUrl.IsActive

            };
            await _urlMappingService.UpdateUrlAsync(urlMapping);
            return Ok(new UrlMappingResponse
            {
                Id = existingUrl.Id,
                ShortCode = existingUrl.ShortCode,
                OriginalUrl = existingUrl.OriginalUrl,
                Title = existingUrl.Title,
                Description = existingUrl.Description,
                ExpiresAt = existingUrl.ExpiresAt,
                IsActive = existingUrl.IsActive,
                ClickCount = existingUrl.ClickCount
            });

        }
        [HttpGet]
        public async Task<ActionResult<IEnumerable<UrlMappingResponse>>> GetAllUrls()
        {
            var urlMappings = await _urlMappingService.GetAllUrlsAsync();
            return Ok(urlMappings.Select(um => new UrlMappingResponse
            {
                Id = um.Id,
                OriginalUrl = um.OriginalUrl,
                ShortCode = um.ShortCode,
                ShortUrl = $"https//:localhost/{um.ShortCode}",
                CreatedAt = um.CreatedAt,
                ClickCount = um.ClickCount,
                ExpiresAt = um.ExpiresAt,
                IsActive = um.IsActive,
                Title = um.Title,
                Description = um.Description
            }));
        }
        [HttpGet("/{id}")]
        public async Task<ActionResult<UrlMappingResponse>> GetUrlById(int Id)
        {
            var existingUrl = await _urlMappingService.GetByIdAsync(Id);
            if (existingUrl == null)
            {
                return NotFound($"URL with ID {Id} not found.");
            }
            return Ok(new UrlMappingResponse
            {
                Id = existingUrl.Id,
                ShortCode = existingUrl.ShortCode,
                OriginalUrl = existingUrl.OriginalUrl,
                Title = existingUrl.Title,
                Description = existingUrl.Description,
                ExpiresAt = existingUrl.ExpiresAt,
                IsActive = existingUrl.IsActive,
                ClickCount = existingUrl.ClickCount
            });
        }
        [HttpGet("/MostClicked/{limit}")]
        public async Task<ActionResult<IEnumerable<UrlMappingResponse>>> GetMostClickedUrl(int limit)
        {
            if (limit <= 0 || limit > 100)
            {
                return BadRequest("Limit must be between 1 and 100");
            }
            try
            {
                var popularUrls = await _urlMappingService.GetMostClickedUrlsAsync(limit);
                var response = popularUrls.Select(um => new UrlMappingResponse
                {
                    Id = um.Id,
                    OriginalUrl = um.OriginalUrl,
                    ShortUrl = $"/{um.ShortCode}",
                    ShortCode = um.ShortCode,
                    Title = um.Title,
                    Description = um.Description,
                    ExpiresAt = um.ExpiresAt,
                    ClickCount = um.ClickCount,
                    IsActive = um.IsActive,
                    CreatedAt = um.CreatedAt
                });

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching most clicked URLs");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("/allActiveUrls")]
        public async Task<ActionResult<IEnumerable<UrlMappingResponse>>> GetActiveUrls()
        {
            try
            {
                var ActiveUrls = await _urlMappingService.GetActiveUrlsAsync();
                var response = ActiveUrls.Select(um => new UrlMappingResponse
                {
                    Id = um.Id,
                    OriginalUrl = um.OriginalUrl,
                    ShortUrl = $"/{um.ShortCode}",
                    ShortCode = um.ShortCode,
                    Title = um.Title,
                    Description = um.Description,
                    ExpiresAt = um.ExpiresAt,
                    ClickCount = um.ClickCount,
                    IsActive = um.IsActive,
                    CreatedAt = um.CreatedAt
                });

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching most Active URLs");
                return StatusCode(500, "Internal server error");
            }
        }
        [HttpGet("{shortCode}")]
        public async Task<IActionResult> RedirectToOriginalUrl(string shortCode)
        {
            try
            {
                var originalUrl = await _urlMappingService.RedirectToOriginalUrlAsync(shortCode);
                
                if (string.IsNullOrEmpty(originalUrl))
                    return NotFound("Short URL not found");
                
                return Redirect(originalUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Redirection failed for {ShortCode}", shortCode);
                return StatusCode(500, "Redirection error");
            }
        }
    }
}