
using System.Threading.Tasks;
using Domain.Entities;
using Moq;
using Infrastructure.Repositories;
using Infrastructure.Data;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore; // Add this line or repApplicationDbContextlace with the correct namespace for ApplicationDbContext

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
        var newUrlMapping = await _repository.AddAsync(urlMapping);
        //assert
        Assert.NotNull(newUrlMapping);
        Assert.Equal(urlMapping.ShortCode, newUrlMapping.ShortCode);
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

        await _repository.AddAsync(urlMapping);
        await _context.SaveChangesAsync(); 

        var addedEntity = await _context.UrlMappings.FindAsync(1);

        // Act 

        await _repository.DeleteAsync(1);
        await _context.SaveChangesAsync(); // Save changes after deletion

        // Assert
        var deletedEntity = await _context.UrlMappings.FindAsync(1);
        Assert.Null(deletedEntity);
        Assert.NotNull(addedEntity);
        /*first we create a new Entity then we added to the In-memory db and save changes 
        the we save the created entity to make sure addasync method worked 
        we implement the deleteasync to delete entity with the id of 1 
        then we check the database for the entity and save it in the deletedentity 
        if it's deleted the variabe will be empty if not it won't be empty */
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateUrlMapping_WhenUrlMappingIsNotNull()
    {
        // Arrange
        var urlMapping = new UrlMapping
        {
            Id = 1,
            ShortCode = "http://Test.com",
            OriginalUrl = "http://finaleTest.com//jshe/fskjvie/hv",
            CreatedAt = DateTime.UtcNow
        };
        await _repository.AddAsync(urlMapping);
        await _context.SaveChangesAsync();

        // Act
        string UpdatedOriginalUrl = "http://monkeyAtTheZoo.com/inf/";
        urlMapping.OriginalUrl = UpdatedOriginalUrl;
        await _repository.UpdateAsync(urlMapping);
        await _context.SaveChangesAsync();

        // Assert
        var updatedUrlMapping = await _context.UrlMappings.FindAsync(1);
        Assert.NotNull(updatedUrlMapping);
        Assert.Equal(UpdatedOriginalUrl, updatedUrlMapping.OriginalUrl);
        
         /* here we are testing if the update method correctly updates an existing record
       we first insert a new url mapping to the database, then change the OriginalUrl 
       value and call UpdateAsync, finally we assert the change by comparing the 
       new value with the one stored in the db */
    }

    [Fact]
    public async Task UpdateAsync_ShouldThrowException_WhenUrlMappingIsNull()
    {
        // Arrange
        UrlMapping nullUrlMapping = null!;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _repository.UpdateAsync(nullUrlMapping)
        );

        /* here we test the repository behavior when trying to update a null object
        the repository is expected to throw an ArgumentNullException */
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllUrlMappings()
    {
        // Arrange
        var urlMapping1 = new UrlMapping
        {
            Id = 1,
            ShortCode = "shortUrl1",
            OriginalUrl = "http://ThisTestIsGood.com",
            CreatedAt = DateTime.UtcNow
        };
        var urlMapping2 = new UrlMapping
        {
            Id = 2,
            ShortCode = "shortUrl2",
            OriginalUrl = "http://ThisTestIsAlsoGood.com",
            CreatedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(urlMapping1);
        await _repository.AddAsync(urlMapping2);
        await _context.SaveChangesAsync();

        // Act
        var allMappings = await _repository.GetAllAsync();

        // Assert
        Assert.NotNull(allMappings);
        Assert.Equal(2, allMappings.Count());
        
        /* we are testing if GetAllAsync returns all url mappings in the db
        we first add two entities and then retrieve them using the method and 
        check if the count equals 2 */
    }
    [Fact]
    public async Task GetByIdUrlAsync_ShouldReturnUrlMapping_WhenIdExists()
    {
        // Arrange
        var urlMapping = new UrlMapping
        {
            Id = 1,
            ShortCode = "shortUrl",
            OriginalUrl = "http://example.com",
            CreatedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(urlMapping);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdUrlAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(urlMapping.Id, result.Id);
        /* here we add a new url mapping to the database and try to fetch it using its id
        we expect GetByIdUrlAsync to return the correct entity */
    }
    [Fact]
    public async Task GetByIdUrlAsync_ShouldReturnNull_WhenIdDoesNotExist()
    {
        // Arrange
        int nonExistentId = 54; // Assuming this ID does not exist in the database
        // Act
        var result = await _repository.GetByIdUrlAsync(nonExistentId);

        // Assert
        Assert.Null(result);
        /* this test checks the behavior of the GetByIdUrlAsync method when the 
        requested id does not exist. it should return null */
    }
    [Fact]
    public async Task GetActiveAsync_ShouldReturnOnlyActiveUrlMappings()
    {
        // Arrange
        var activeUrlMapping = new UrlMapping
        {
            Id = 1,
            ShortCode = "activeShortUrl",
            OriginalUrl = "http://TheUrlIsActive.com",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        var inactiveUrlMapping = new UrlMapping
        {
            Id = 2,
            ShortCode = "inactiveShortUrl",
            OriginalUrl = "http://TheUrlIsInactive.com",
            IsActive = false,
            CreatedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(activeUrlMapping);
        await _repository.AddAsync(inactiveUrlMapping);
        await _context.SaveChangesAsync();

        // Act
        var activeMappings = await _repository.GetActiveAsync();

        // Assert
        Assert.Single(activeMappings);
        Assert.Contains(activeUrlMapping, activeMappings);

    }

    [Fact]
    public async Task GetActiveAsync_ShouldReturnEmpty_WhenNoActiveMappingsExist()
    {
        // Arrange
        var inactiveUrlMapping = new UrlMapping
        {
            Id = 1,
            ShortCode = "inactiveShortUrl",
            OriginalUrl = "http://TheUrlIsInactive.com",
            IsActive = false,
            CreatedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(inactiveUrlMapping);
        await _context.SaveChangesAsync();

        // Act
        var activeMappings = await _repository.GetActiveAsync();

        // Assert
        Assert.Empty(activeMappings);
    }

    [Fact]
    public async Task GetMostClickedAsync_ShouldReturnMostClickedUrlMappings()
    {
        // Arrange
        var urlMapping1 = new UrlMapping
        {
            Id = 1,
            ShortCode = "shortUrl1",
            OriginalUrl = "http://ClickThisUrl.com",
            ClickCount = 10,
            CreatedAt = DateTime.UtcNow
        };
        var urlMapping2 = new UrlMapping
        {
            Id = 2,
            ShortCode = "shortUrl2",
            OriginalUrl = "http://ClickThisUrl2.com",
            ClickCount = 20,
            CreatedAt = DateTime.UtcNow
        };
        var urlMapping3 = new UrlMapping
        {
            Id = 3,
            ShortCode = "shortUrl3",
            OriginalUrl = "http://ClickThisUrl3.com",
            ClickCount = 5,
            CreatedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(urlMapping1);
        await _repository.AddAsync(urlMapping2);
        await _repository.AddAsync(urlMapping3);
        await _context.SaveChangesAsync();

        // Act
        var mostClickedMappings = await _repository.GetMostClickedAsync(2);

        // Assert
        Assert.Equal(2, mostClickedMappings.Count());
        Assert.Contains(mostClickedMappings, x => x.Id == urlMapping2.Id);
        Assert.Contains(mostClickedMappings, x => x.Id == urlMapping1.Id);
    }
    [Fact]
    public async Task GetMostClickedAsync_ShouldReturnEmpty_WhenNoMappingsExist()
    {
        // Act
        var mostClickedMappings = await _repository.GetMostClickedAsync(5);

        // Assert
        Assert.Empty(mostClickedMappings);
    }
    [Fact]
    public async Task GetMostClickedAsync_ShouldReturnEmpty_WhenLimitIsZero()
    {
        // Arrange
        var urlMapping1 = new UrlMapping
        {
            Id = 1,
            ShortCode = "shortUrl",
            OriginalUrl = "http://mostlicked1.com",
            ClickCount = 10,
            CreatedAt = DateTime.UtcNow
        };
        var urlMapping2 = new UrlMapping
        {
            Id = 2,
            ShortCode = "shortUrl",
            OriginalUrl = "http://mostlicked2.com",
            ClickCount = 10,
            CreatedAt = DateTime.UtcNow
        };
        var urlMapping3 = new UrlMapping
        {
            Id = 3,
            ShortCode = "shortUrl",
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
        var mostClickedMappings = await _repository.GetMostClickedAsync(5);

        // Assert
        Assert.Empty(mostClickedMappings);
    }
    
    
}
