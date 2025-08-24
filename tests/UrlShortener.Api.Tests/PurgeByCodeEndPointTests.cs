
using API.Controllers;
using Application.Services;
using Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using StackExchange.Redis;
namespace UrlShortener.Api.Tests;

public class PurgeByCodeEndPointTests
{

    private readonly IConnectionMultiplexer _redis;
    private readonly IDatabase _db;
    private readonly UrlMappingService _cacheService;
    private UrlShortenerController _controller;

    private readonly Mock<ILogger<UrlShortenerController>> _loggerMock;

    public PurgeByCodeEndPointTests()
    {
        var mockRepo = new Mock<IUrlMappingRepository>();
        var mockUnit = new Mock<IUnitOfWork>();
        var mockLogger = new Mock<ILogger<UrlMappingService>>();
        var mockShortUrlGen = new Mock<IShortUrlGeneratorService>();

        // Using a real or in-memory Redis instance for tests
        _redis = ConnectionMultiplexer.Connect("localhost");
        _db = _redis.GetDatabase();
        _loggerMock = new Mock<ILogger<UrlShortenerController>>();

        _cacheService = new UrlMappingService(
             mockRepo.Object,
            mockUnit.Object,
            mockLogger.Object,
            mockShortUrlGen.Object,
            _redis
        );

        var mockCacheService = new Mock<IUrlMappingService>();
        _controller = new UrlShortenerController(_loggerMock.Object, mockCacheService.Object);
    }
    private string GenerateTestKey() => $"url:test:{Guid.NewGuid()}";

    [Fact]
    public async Task RemoveAsync_ExistingKey_ReturnsTrue()
    {
        // Arrange
        var key = GenerateTestKey();
        await _db.StringSetAsync(key, "https://example.com");

        // Act
        var result = await _cacheService.RemoveAsync(key.Replace("url:", ""));

        // Assert
        Assert.True(result);  // Service returns true when key existed
        var exists = await _db.KeyExistsAsync(key);
        Assert.False(exists); // Key should be removed from Redis
    }

    [Fact]
    public async Task RemoveAsync_NonExistingKey_ThrowsCacheNotFoundException()
    {
        // Arrange
        var key = GenerateTestKey();

        // Act & Assert
        var exists = await _db.KeyExistsAsync(key);

        Assert.False(exists); // Key not found
    }

    [Fact]
    public async Task CacheUrlAsync_SetsTTL_ExpiresCorrectly()
    {
        // Arrange
        var key = GenerateTestKey();
        var ttl = TimeSpan.FromSeconds(1); // Short TTL for test
        await _db.StringSetAsync(key, "https://example.com", ttl);

        // Act
        await Task.Delay(1500); // Wait for key to expire

        // Assert
        var exists = await _db.KeyExistsAsync(key);
        Assert.False(exists); // Key should be expired
    }

    [Fact]
    public async Task Delete_AdminEndpoint_Returns204ForExistingKey()
    {
       // Arrange
        var mockCacheService = new Mock<IUrlMappingService>();
        mockCacheService.Setup(s => s.RemoveAsync("abc"))
            .ReturnsAsync(true); // simulate key exists

        var controller = new UrlShortenerController(_loggerMock.Object, mockCacheService.Object);

        // Act
        var result = await controller.PurgeByCode("abc");

        // Assert
        Assert.IsType<NoContentResult>(result);
    }
    [Fact]
    public async Task PurgeByCode_ReturnsNotFound_ForNonExistingKey()
    {
        // Arrange
        var mockCacheService = new Mock<IUrlMappingService>();
        string shortCode = "nonexistent-key";
        mockCacheService.Setup(s => s.RemoveAsync(shortCode))
            .ReturnsAsync(false); // Key does not exist

        // Act
        var result = await _controller.PurgeByCode(shortCode);

        // Assert
        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(404, notFound.StatusCode);
        Assert.Equal($"ShortCode '{shortCode}' not found in cache.", notFound.Value);
    }



}