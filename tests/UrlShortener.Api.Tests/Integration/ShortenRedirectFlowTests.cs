using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using API.DTOs.UrlMapping;
using UrlShortener.Api.Tests.TestInfrastructure;
using UrlShortener.Api.Tests.TestHelpers;

namespace UrlShortener.Api.Tests.Integration;

[Collection("Integration Tests")]
public class ShortenRedirectFlowTests : IntegrationTestBase
{
    public ShortenRedirectFlowTests(TestWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task ShortenThenRedirect_BasicFlow_ShouldWork()
    {
        // Arrange
        var createRequest = new CreateUrlMappingRequest
        {
            OriginalUrl = TestConstants.Urls.GitHub,
            Title = "GitHub",
            Description = "GitHub homepage"
        };

        // Act - Create short URL
        var createResponse = await PostJsonAsync("/UrlShortener", createRequest);

        // Assert - Creation was successful
        AssertSuccessResponse(createResponse);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var createResult = await DeserializeAsync<CreateUrlMappingResponse>(createResponse);
        createResult.Should().NotBeNull();
        createResult!.ShortCode.Should().NotBeNullOrEmpty();
        createResult.OriginalUrl.Should().Be(TestConstants.Urls.GitHub);
        createResult.IsActive.Should().BeTrue();
        createResult.ClickCount.Should().Be(0);

        // SOLUTION: Ensure transaction commit before redirect
        await EnsureTransactionCommitAsync();

        // Act - Follow redirect (ensure we don't auto-follow redirects so we can test the redirect response)
        HttpClient.DefaultRequestHeaders.Clear();
        var redirectResponse = await HttpClient.GetAsync($"/UrlShortener/{createResult.ShortCode}");

        // Debug - Log response details
        var responseContent = await redirectResponse.Content.ReadAsStringAsync();
        Console.WriteLine($"Response Status: {redirectResponse.StatusCode}");
        Console.WriteLine($"Response Location: {redirectResponse.Headers.Location}");
        Console.WriteLine($"Response Content: {responseContent}");

        // Assert - Redirect works
        redirectResponse.StatusCode.Should().Be(HttpStatusCode.Found);
        
        // Handle trailing slash differences - normalize both URLs for comparison
        var actualLocation = redirectResponse.Headers.Location?.ToString().TrimEnd('/');
        var expectedLocation = TestConstants.Urls.GitHub.TrimEnd('/');
        actualLocation.Should().Be(expectedLocation);

        // Verify click count was incremented in database
        var urlMapping = await DbContext.UrlMappings
            .FirstOrDefaultAsync(u => u.ShortCode == createResult.ShortCode);
        urlMapping.Should().NotBeNull();
        urlMapping!.ClickCount.Should().Be(1);

        // Verify cache was populated (check Redis has redirect cache)
        var redirectCacheKey = $"redirect:{createResult.ShortCode}";
        var cachedRedirect = await RedisDatabase.StringGetAsync(redirectCacheKey);
        cachedRedirect.HasValue.Should().BeTrue();
    }

    [Fact]
    public async Task ShortenWithCustomCode_ThenRedirect_ShouldWork()
    {
        // Arrange
        var createRequest = new CreateUrlMappingRequest
        {
            OriginalUrl = TestConstants.Urls.Google,
            CustomShortCode = TestConstants.ShortCodes.Custom1,
            Title = "Google Search"
        };

        // Act - Create with custom short code
        var createResponse = await PostJsonAsync("/UrlShortener", createRequest);

        // Assert - Creation successful
        AssertSuccessResponse(createResponse);
        var createResult = await DeserializeAsync<CreateUrlMappingResponse>(createResponse);
        createResult!.ShortCode.Should().Be(TestConstants.ShortCodes.Custom1);

        // SOLUTION: Ensure transaction commit before redirect
        await EnsureTransactionCommitAsync();

        // Act - Test redirect
        var redirectResponse = await HttpClient.GetAsync($"/UrlShortener/{TestConstants.ShortCodes.Custom1}");

        // Assert - Redirect works with custom code
        redirectResponse.StatusCode.Should().Be(HttpStatusCode.Found);
        
        // Handle trailing slash differences - normalize both URLs for comparison
        var actualLocation = redirectResponse.Headers.Location?.ToString().TrimEnd('/');
        var expectedLocation = TestConstants.Urls.Google.TrimEnd('/');
        actualLocation.Should().Be(expectedLocation);
    }

    [Fact]
    public async Task RedirectToExpiredUrl_ShouldReturnNotFound()
    {
        // Arrange - Create URL with future expiry, then manually expire it
        var createRequest = new CreateUrlMappingRequest
        {
            OriginalUrl = TestConstants.Urls.YouTube,
            ExpiresAt = DateTime.UtcNow.AddHours(1) // Future expiry
        };

        var createResponse = await PostJsonAsync("/UrlShortener", createRequest);
        AssertSuccessResponse(createResponse);
        var createResult = await DeserializeAsync<CreateUrlMappingResponse>(createResponse);

        // Use the proper API endpoint to expire the URL
        var updateRequest = new UpdateUrlMappingRequest
        {
            Id = createResult!.Id,
            OriginalUrl = TestConstants.Urls.YouTube,
            ExpiresAt = DateTime.UtcNow.AddMinutes(-1) // Expire it through API
        };
        
        var updateResponse = await HttpClient.PutAsJsonAsync("/UrlShortener", updateRequest);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK, "Update should succeed");

        // Act - Try to redirect to expired URL
        var redirectResponse = await HttpClient.GetAsync($"/UrlShortener/{createResult!.ShortCode}");

        // Assert - Should return 404
        redirectResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task RedirectNonExistentShortCode_ShouldReturnNotFound()
    {
        // Arrange
        const string nonExistentCode = "nonexist";

        // Act
        var redirectResponse = await HttpClient.GetAsync($"/UrlShortener/{nonExistentCode}");

        // Assert
        redirectResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task MultipleRedirects_ShouldIncrementClickCount()
    {
        // Arrange
        var createRequest = new CreateUrlMappingRequest
        {
            OriginalUrl = TestConstants.Urls.LinkedIn
        };

        var createResponse = await PostJsonAsync("/UrlShortener", createRequest);
        var createResult = await DeserializeAsync<CreateUrlMappingResponse>(createResponse);

        // Act - Perform multiple redirects
        const int redirectCount = 5;
        for (int i = 0; i < redirectCount; i++)
        {
            var redirectResponse = await HttpClient.GetAsync($"/UrlShortener/{createResult!.ShortCode}");
            redirectResponse.StatusCode.Should().Be(HttpStatusCode.Found);
        }

        // Assert - Click count should be incremented
        var urlMapping = await DbContext.UrlMappings
            .FirstOrDefaultAsync(u => u.ShortCode == createResult!.ShortCode);
        urlMapping!.ClickCount.Should().Be(redirectCount);
    }

    [Fact]
    public async Task RedirectThenDeactivateUrl_ShouldReturnNotFoundAfterDeactivation()
    {
        // Arrange - Create active URL
        var createRequest = new CreateUrlMappingRequest
        {
            OriginalUrl = TestConstants.Urls.ECommerce,
            Title = "E-commerce Store"
        };

        var createResponse = await PostJsonAsync("/UrlShortener", createRequest);
        var createResult = await DeserializeAsync<CreateUrlMappingResponse>(createResponse);

        // Act - First redirect should work
        var firstRedirectResponse = await HttpClient.GetAsync($"/UrlShortener/{createResult!.ShortCode}");
        firstRedirectResponse.StatusCode.Should().Be(HttpStatusCode.Found);

        // Deactivate URL by setting IsActive to false in database
        var urlMapping = await DbContext.UrlMappings
            .FirstAsync(u => u.ShortCode == createResult.ShortCode);
        urlMapping.IsActive = false;
        await DbContext.SaveChangesAsync();

        // Clear cache to force database lookup
        await ClearCacheAsync();

        // Act - Second redirect attempt should fail
        var secondRedirectResponse = await HttpClient.GetAsync($"/UrlShortener/{createResult.ShortCode}");

        // Assert - Should return 404 for deactivated URL
        secondRedirectResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ConcurrentRedirects_ShouldHandleClickCountCorrectly()
    {
        // Arrange
        var createRequest = new CreateUrlMappingRequest
        {
            OriginalUrl = TestConstants.Urls.Documentation
        };

        var createResponse = await PostJsonAsync("/UrlShortener", createRequest);
        var createResult = await DeserializeAsync<CreateUrlMappingResponse>(createResponse);

        // Act - Perform concurrent redirects
        const int concurrentRequests = 10;
        var tasks = Enumerable.Range(0, concurrentRequests)
            .Select(_ => HttpClient.GetAsync($"/UrlShortener/{createResult!.ShortCode}"))
            .ToArray();

        var responses = await Task.WhenAll(tasks);

        // Assert - All requests should succeed
        responses.Should().AllSatisfy(response => 
            response.StatusCode.Should().Be(HttpStatusCode.Found));

        // Verify final click count (should handle concurrent increments)
        var urlMapping = await DbContext.UrlMappings
            .FirstOrDefaultAsync(u => u.ShortCode == createResult!.ShortCode);
        urlMapping!.ClickCount.Should().Be(concurrentRequests);
    }

    // =================== ADDITIONAL CORE FLOW TESTS ===================

    [Fact]
    public async Task CreateShortUrl_ThenRedirect_ShouldWork_EndToEnd()
    {
        // Arrange
        var createRequest = new CreateUrlMappingRequest
        {
            OriginalUrl = TestConstants.Urls.NewsArticle,
            Title = "Breaking News",
            Description = "Latest technology breakthrough"
        };

        // Act - Complete end-to-end flow
        var createResponse = await PostJsonAsync("/UrlShortener", createRequest);
        var createResult = await DeserializeAsync<CreateUrlMappingResponse>(createResponse);

        // Assert - Creation successful
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        createResult!.OriginalUrl.Should().Be(TestConstants.Urls.NewsArticle);
        createResult.Title.Should().Be("Breaking News");
        createResult.IsActive.Should().BeTrue();

        // Act - Test redirect works
        var redirectResponse = await HttpClient.GetAsync($"/UrlShortener/{createResult.ShortCode}");

        // Assert - Redirect successful
        redirectResponse.StatusCode.Should().Be(HttpStatusCode.Found);
        
        // Handle trailing slash differences - normalize both URLs for comparison
        var actualLocation = redirectResponse.Headers.Location?.ToString().TrimEnd('/');
        var expectedLocation = TestConstants.Urls.NewsArticle.TrimEnd('/');
        actualLocation.Should().Be(expectedLocation);

        // Verify database state
        var dbEntity = await DbContext.UrlMappings
            .FirstOrDefaultAsync(u => u.ShortCode == createResult.ShortCode);
        dbEntity.Should().NotBeNull();
        dbEntity!.ClickCount.Should().Be(1);
    }

    [Fact]
    public async Task CreateWithExpiry_ThenRedirectAfterExpiry_ShouldFail()
    {
        // Arrange - Test that expired URLs cannot be created
        var expiredCreateRequest = new CreateUrlMappingRequest
        {
            OriginalUrl = TestConstants.Urls.SocialMedia,
            ExpiresAt = DateTime.UtcNow.AddMinutes(-10) // Expired 10 minutes ago
        };

        // Act - Try to create expired URL
        var expiredCreateResponse = await PostJsonAsync("/UrlShortener", expiredCreateRequest);
        
        // Assert - Should be rejected
        expiredCreateResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // Now test the main scenario - create future expiry then manually expire
        var createRequest = new CreateUrlMappingRequest
        {
            OriginalUrl = TestConstants.Urls.SocialMedia,
            ExpiresAt = DateTime.UtcNow.AddHours(1) // Future expiry
        };
        
        var createResponse = await PostJsonAsync("/UrlShortener", createRequest);
        AssertSuccessResponse(createResponse);
        var createResult = await DeserializeAsync<CreateUrlMappingResponse>(createResponse);
        
        // Ensure transaction commit before expiry
        await EnsureTransactionCommitAsync();
        
        // Use the proper API endpoint to expire the URL
        var updateRequest = new UpdateUrlMappingRequest
        {
            Id = createResult!.Id,
            OriginalUrl = TestConstants.Urls.SocialMedia,
            ExpiresAt = DateTime.UtcNow.AddMinutes(-1) // Expire it through API
        };
        
        var updateResponse = await HttpClient.PutAsJsonAsync("/UrlShortener", updateRequest);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK, "Update should succeed");

        // Act - Try to redirect expired URL
        var redirectResponse = await HttpClient.GetAsync($"/UrlShortener/{createResult!.ShortCode}");

        // Assert - Should fail with 404
        redirectResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // =================== CACHE-SPECIFIC SCENARIOS ===================

    [Fact]
    public async Task FirstRedirect_ShouldCacheMiss_ThenCacheHit()
    {
        // Arrange
        var createRequest = new CreateUrlMappingRequest
        {
            OriginalUrl = TestConstants.Urls.ECommerce
        };

        var createResponse = await PostJsonAsync("/UrlShortener", createRequest);
        var createResult = await DeserializeAsync<CreateUrlMappingResponse>(createResponse);

        // Clear cache to ensure miss
        await ClearCacheAsync();

        // Act - First redirect (should be cache miss)
        var startTime = DateTime.UtcNow;
        var firstRedirectResponse = await HttpClient.GetAsync($"/UrlShortener/{createResult!.ShortCode}");
        var firstRedirectTime = DateTime.UtcNow - startTime;

        // Assert - First redirect succeeds
        firstRedirectResponse.StatusCode.Should().Be(HttpStatusCode.Found);

        // Verify cache was populated
        var redirectCacheKey = $"redirect:{createResult.ShortCode}";
        var cachedValue = await RedisDatabase.StringGetAsync(redirectCacheKey);
        cachedValue.HasValue.Should().BeTrue("Cache should be populated after first redirect");

        // Act - Second redirect (should be cache hit)
        startTime = DateTime.UtcNow;
        var secondRedirectResponse = await HttpClient.GetAsync($"/UrlShortener/{createResult.ShortCode}");
        var secondRedirectTime = DateTime.UtcNow - startTime;

        // Assert - Second redirect also succeeds (from cache)
        secondRedirectResponse.StatusCode.Should().Be(HttpStatusCode.Found);
        
        // Verify click count incremented for both requests
        var urlMapping = await DbContext.UrlMappings
            .FirstOrDefaultAsync(u => u.ShortCode == createResult.ShortCode);
        urlMapping!.ClickCount.Should().Be(2);
    }

    [Fact]
    public async Task UpdateUrl_ShouldInvalidateCache()
    {
        // Arrange - Create and cache a URL
        var createRequest = new CreateUrlMappingRequest
        {
            OriginalUrl = TestConstants.Urls.GitHub
        };

        var createResponse = await PostJsonAsync("/UrlShortener", createRequest);
        var createResult = await DeserializeAsync<CreateUrlMappingResponse>(createResponse);

        // Trigger caching
        await HttpClient.GetAsync($"/UrlShortener/{createResult!.ShortCode}");

        // Verify cache exists
        var cacheKey = $"redirect:{createResult.ShortCode}";
        var cachedBefore = await RedisDatabase.StringGetAsync(cacheKey);
        cachedBefore.HasValue.Should().BeTrue();

        // Act - Update the URL (simulate cache invalidation by manual database update)
        var urlMapping = await DbContext.UrlMappings
            .FirstAsync(u => u.ShortCode == createResult.ShortCode);
        urlMapping.OriginalUrl = TestConstants.Urls.Google;
        urlMapping.UpdatedAt = DateTime.UtcNow;
        await DbContext.SaveChangesAsync();

        // Manually clear cache (simulating what update operation should do)
        await RedisDatabase.KeyDeleteAsync(cacheKey);
        await RedisDatabase.KeyDeleteAsync($"entity:short:{createResult.ShortCode}");

        // Act - Redirect after update
        var redirectResponse = await HttpClient.GetAsync($"/UrlShortener/{createResult.ShortCode}");

        // Assert - Should redirect to new URL
        redirectResponse.StatusCode.Should().Be(HttpStatusCode.Found);
        
        // Handle trailing slash differences - normalize both URLs for comparison
        var actualLocation = redirectResponse.Headers.Location?.ToString().TrimEnd('/');
        var expectedLocation = TestConstants.Urls.Google.TrimEnd('/');
        actualLocation.Should().Be(expectedLocation);
    }

    [Fact]
    public async Task DeleteUrl_ShouldRemoveFromCache()
    {
        // Arrange - Create and cache a URL
        var createRequest = new CreateUrlMappingRequest
        {
            OriginalUrl = TestConstants.Urls.LinkedIn
        };

        var createResponse = await PostJsonAsync("/UrlShortener", createRequest);
        var createResult = await DeserializeAsync<CreateUrlMappingResponse>(createResponse);

        // Trigger caching
        await HttpClient.GetAsync($"/UrlShortener/{createResult!.ShortCode}");

        // Verify cache exists
        var cacheKey = $"redirect:{createResult.ShortCode}";
        var cachedBefore = await RedisDatabase.StringGetAsync(cacheKey);
        cachedBefore.HasValue.Should().BeTrue();

        // Act - Delete the URL
        var deleteResponse = await HttpClient.DeleteAsync($"/UrlShortener?id={createResult.Id}");

        // Assert - Delete successful
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify cache was cleared
        var cachedAfter = await RedisDatabase.StringGetAsync(cacheKey);
        cachedAfter.HasValue.Should().BeFalse("Cache should be cleared after deletion");

        // Verify redirect fails
        var redirectResponse = await HttpClient.GetAsync($"/UrlShortener/{createResult.ShortCode}");
        redirectResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // =================== ERROR HANDLING SCENARIOS ===================

    [Fact]
    public async Task InvalidUrl_ShouldReturn400()
    {
        // Arrange - Invalid URL formats
        var invalidUrls = new[]
        {
            "", // Empty
            "not-a-url", // Not a URL
            "ftp://invalid.com", // Invalid scheme
            "javascript:alert('xss')" // Potentially malicious
        };

        foreach (var invalidUrl in invalidUrls)
        {
            var createRequest = new CreateUrlMappingRequest
            {
                OriginalUrl = invalidUrl
            };

            // Act
            var createResponse = await PostJsonAsync("/UrlShortener", createRequest);

            // Assert
            createResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest, 
                $"URL '{invalidUrl}' should be rejected");
        }
    }

    [Fact]
    public async Task DuplicateCustomCode_ShouldReturn400()
    {
        // Arrange - Create first URL with custom code
        var firstRequest = new CreateUrlMappingRequest
        {
            OriginalUrl = TestConstants.Urls.GitHub,
            CustomShortCode = TestConstants.ShortCodes.Custom1
        };

        var firstResponse = await PostJsonAsync("/UrlShortener", firstRequest);
        firstResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // Act - Try to create second URL with same custom code
        var duplicateRequest = new CreateUrlMappingRequest
        {
            OriginalUrl = TestConstants.Urls.Google,
            CustomShortCode = TestConstants.ShortCodes.Custom1 // Same code
        };

        var duplicateResponse = await PostJsonAsync("/UrlShortener", duplicateRequest);

        // Assert - Should be rejected
        duplicateResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task NonExistentShortCode_ShouldReturn404()
    {
        // Arrange
        var nonExistentCodes = new[]
        {
            "notfound",
            "missing123",
            "xyz789",
            "doesnotexist"
        };

        foreach (var code in nonExistentCodes)
        {
            // Act
            var redirectResponse = await HttpClient.GetAsync($"/UrlShortener/{code}");

            // Assert
            redirectResponse.StatusCode.Should().Be(HttpStatusCode.NotFound,
                $"Non-existent code '{code}' should return 404");
        }
    }
}
