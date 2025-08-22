using System.Threading.Tasks;
using Domain.Entities;
using Moq;
using Infrastructure.Repositories;
using Infrastructure.Data;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

namespace UrlShortener.Infrastructure.Tests;

public class UrlMappingRepositoryTest
{
    private readonly ApplicationDbContext _context;
    private readonly UrlMappingRepository _repository;
    private readonly Mock<ILogger<UrlMappingRepository>> _loggerMock;
    
    //we are using an in-memory database for repository testing which is considered the best practice when working with EF Core
    public UrlMappingRepositoryTest()
    {
        var Options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()) // Unique DB per test
            .Options;
        _loggerMock = new Mock<ILogger<UrlMappingRepository>>();
        _context = new ApplicationDbContext(Options);
        _repository = new UrlMappingRepository(_context, _loggerMock.Object);
    }
    
    [Fact]
    public async Task AddAsync_ShouldReturnUrlMapping_WhenTheGivenUrlMappingIsNotNull()
    {
        //arrange
        var urlMapping = new UrlMapping
        {
            Id = 1,
            ShortCode = "shortUrl",
            OriginalUrl = "http://example.com",
            CreatedAt = DateTime.UtcNow
        };
        
        // Act
        var result = await _repository.AddAsync(urlMapping);
        
        //assert
        Assert.NotNull(result.Value);
        Assert.Equal(urlMapping.ShortCode, result.Value.ShortCode);
        
        /*we will create a new urlmapping and usign the addasync it 
        will be created in the database 
        in the assert we are checking if the shortCode in the urlmapping entity is the same 
        as the urlmapping that was created in the database */
    }
    
    [Fact]
    public async Task AddAsync_ShouldThrowException_WhenUrlMappingIsNull()
    {
        // Arrange
        UrlMapping nullUrlMapping = null!;
        
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _repository.AddAsync(nullUrlMapping)
        );
        
        /*here we pass a null url to the addasync method to make sure it return the ArgumentNullException*/
    }
    
    [Fact]
    public async Task DeleteAsync_ShouldRemove_WhatWasAdded()
    {
        // Arrange
        var urlMapping = new UrlMapping
        {
            Id = 1,
            ShortCode = "test",
            OriginalUrl = "http://ReadyToBeDeleted.com",
            CreatedAt = DateTime.UtcNow
        };
        
        var addResult = await _repository.AddAsync(urlMapping);
        await _context.SaveChangesAsync(); 
        
        var addedEntity = await _context.UrlMappings.FindAsync(1);
        
        // Act 
        await _repository.DeleteAsync(1);
        await _context.SaveChangesAsync(); // Save changes after deletion
        
        // Assert
        var deletedEntity = await _context.UrlMappings.FindAsync(1);
        Assert.Null(deletedEntity);
        
        /*here we are adding an entity and deleting it and making sure it was deleted*/
    }
    
    [Fact]
    public async Task GetByShortCodeAsync_ShouldReturnUrlMapping_WhenShortCodeExists()
    {
        // Arrange
        var urlMapping = new UrlMapping
        {
            Id = 1,
            ShortCode = "test123",
            OriginalUrl = "http://example.com",
            CreatedAt = DateTime.UtcNow
        };
        
        await _repository.AddAsync(urlMapping);
        await _context.SaveChangesAsync();
        
        // Act
        var result = await _repository.GetByShortCodeAsync("test123");
        
        // Assert
        Assert.NotNull(result.Value);
        Assert.Equal("test123", result.Value.ShortCode);
        Assert.Equal("http://example.com", result.Value.OriginalUrl);
        
        /*Tests that GetByShortCodeAsync returns the correct UrlMapping when given a valid short code*/
    }
    
    [Fact]
    public async Task GetByShortCodeAsync_ShouldReturnNull_WhenShortCodeDoesNotExist()
    {
        // Act
        var result = await _repository.GetByShortCodeAsync("nonexistent");
        
        // Assert
        Assert.Null(result.Value);
        
        /*Tests that GetByShortCodeAsync returns null when the short code doesn't exist in the database*/
    }
    
    [Fact]
    public async Task GetByShortCodeAsync_ShouldThrowException_WhenShortCodeIsNull()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _repository.GetByShortCodeAsync(null!)
        );
        
        /*Tests the behavior of GetByShortCodeAsync when null is passed as shortCode.
        Verifies that it throws an ArgumentNullException.*/
    }
    
    [Fact]
    public async Task GetByShortCodeAsync_ShouldThrowException_WhenShortCodeIsEmpty()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _repository.GetByShortCodeAsync("")
        );
        
        /*Tests the behavior of GetByShortCodeAsync when an empty string is passed as shortCode.
        Verifies that it throws an ArgumentException.*/
    }
    
    [Fact]
    public async Task UpdateAsync_ShouldModifyExistingUrlMapping()
    {
        // Arrange
        var originalMapping = new UrlMapping
        {
            Id = 1,
            ShortCode = "original",
            OriginalUrl = "http://original.com",
            ClickCount = 5,
            CreatedAt = DateTime.UtcNow
        };
        
        await _repository.AddAsync(originalMapping);
        await _context.SaveChangesAsync();
        
        // Modify the mapping
        originalMapping.ClickCount = 10;
        originalMapping.OriginalUrl = "http://updated.com";
        
        // Act
        var result = await _repository.UpdateAsync(originalMapping);
        await _context.SaveChangesAsync();
        
        // Assert
        Assert.NotNull(result.Value);
        Assert.Equal(10, result.Value.ClickCount);
        Assert.Equal("http://updated.com", result.Value.OriginalUrl);
        
        /*Tests that UpdateAsync successfully modifies an existing UrlMapping.
        Verifies that the changes are persisted correctly.*/
    }
    
    [Fact]
    public async Task UpdateAsync_ShouldThrowException_WhenUrlMappingIsNull()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _repository.UpdateAsync(null!)
        );
        
        /*Tests the behavior of UpdateAsync when null is passed as the UrlMapping.
        Verifies that it throws an ArgumentNullException.*/
    }
    
    [Fact]
    public async Task GetMostClickedAsync_ShouldReturnMostClickedMappings()
    {
        // Arrange
        var urlMapping1 = new UrlMapping
        {
            Id = 1,
            ShortCode = "shortUrl1",
            OriginalUrl = "http://mostlicked1.com",
            ClickCount = 15,
            CreatedAt = DateTime.UtcNow
        };
        
        var urlMapping2 = new UrlMapping
        {
            Id = 2,
            ShortCode = "shortUrl2",
            OriginalUrl = "http://mostlicked2.com",
            ClickCount = 10,
            CreatedAt = DateTime.UtcNow
        };
        
        var urlMapping3 = new UrlMapping
        {
            Id = 3,
            ShortCode = "shortUrl3",
            OriginalUrl = "http://mostlicked3.com",
            ClickCount = 20,
            CreatedAt = DateTime.UtcNow
        };
        
        await _repository.AddAsync(urlMapping1);
        await _repository.AddAsync(urlMapping2);
        await _repository.AddAsync(urlMapping3);
        await _context.SaveChangesAsync();
        
        // Act
        var result = await _repository.GetMostClickedAsync(2);
        
        // Assert
        Assert.Equal(2, result.Value.Count());
        Assert.Equal("shortUrl3", result.Value.First().ShortCode); // Should be the most clicked (20)
        Assert.Equal("shortUrl1", result.Value.Skip(1).First().ShortCode); // Second most clicked (15)
        
        /* Tests the GetMostClickedAsync method to ensure it returns the correct number of mappings 
        ordered by click count in descending order.*/
    }
    
    [Fact]
    public async Task GetMostClickedAsync_ShouldThrowException_WhenLimitIsZero()
    {
        // Arrange
        var urlMapping1 = new UrlMapping
        {
            Id = 1,
            ShortCode = "shortUrl1",
            OriginalUrl = "http://mostlicked1.com",
            ClickCount = 15,
            CreatedAt = DateTime.UtcNow
        };
        
        var urlMapping2 = new UrlMapping
        {
            Id = 2,
            ShortCode = "shortUrl2",
            OriginalUrl = "http://mostlicked2.com",
            ClickCount = 10,
            CreatedAt = DateTime.UtcNow
        };
        
        var urlMapping3 = new UrlMapping
        {
            Id = 3,
            ShortCode = "shortUrl3",
            OriginalUrl = "http://mostlicked3.com",
            ClickCount = 10,
            CreatedAt = DateTime.UtcNow
        };
        
        await _repository.AddAsync(urlMapping1);
        await _repository.AddAsync(urlMapping2);
        await _repository.AddAsync(urlMapping3);
        await _context.SaveChangesAsync();
        
        // Act
        var exception = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
                () => _repository.GetMostClickedAsync(0)
        );
        
        // Assert
        Assert.Equal("limit", exception.ParamName);
        Assert.Equal("Limit must be greater than zero. (Parameter 'limit')", exception.Message);
        
        /* Tests the behavior of GetMostClickedAsync when the limit is zero.
        Verifies that it throws an ArgumentOutOfRangeException with the correct message.*/
    }
    
    [Fact]
    public async Task GetMostClickedAsync_ShouldThrowException_WhenLimitIsNegative()
    {
        // Arrange
        int negativeLimit = -5;
        
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => _repository.GetMostClickedAsync(negativeLimit)
        );
        
        /* Tests the behavior of GetMostClickedAsync when the limit is negative.
        Verifies that it throws an ArgumentOutOfRangeException with the correct message.*/
    }
    
    [Fact]
    public async Task GetMostClickedAsync_ShouldReturnEmpty_WhenNoMappingsHaveClicks()
    {
        // Arrange
        var urlMapping1 = new UrlMapping
        {
            Id = 1,
            ShortCode = "shortUrl1",
            OriginalUrl = "http://NoClicks1.com",
            ClickCount = 0,
            CreatedAt = DateTime.UtcNow
        };
        
        var urlMapping2 = new UrlMapping
        {
            Id = 2,
            ShortCode = "shortUrl2",
            OriginalUrl = "http://NoClicks2.com",
            ClickCount = 0,
            CreatedAt = DateTime.UtcNow
        };
        
        await _repository.AddAsync(urlMapping1);
        await _repository.AddAsync(urlMapping2);
        await _context.SaveChangesAsync();
        
        // Act
        var result = await _repository.GetMostClickedAsync(5);
        
        // Assert
        Assert.Empty(result.Value);
        
        /* Tests the scenario where no UrlMappings have clicks.
        Verifies that the method returns an empty collection when all mappings have ClickCount of zero.*/
    }
}
