using System.Threading.Tasks;
using API.Controllers;
using API.DTOs.UrlMapping;
using Application.Services;
using Domain.Entities;
using Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using StackExchange.Redis;
using Xunit;

namespace UrlShortener.Api.Tests
{
    public class UrlShortenerControllerAndServiceTests
    {
        private readonly UrlShortenerController _controller;
        private readonly Mock<IUrlMappingService> _serviceMock;
        private readonly Mock<ILogger<UrlShortenerController>> _loggerMock;

        public UrlShortenerControllerAndServiceTests()
        {
            _serviceMock = new Mock<IUrlMappingService>();
            _loggerMock = new Mock<ILogger<UrlShortenerController>>();
            _controller = new UrlShortenerController(_loggerMock.Object, _serviceMock.Object);
        }

        [Fact]
        public async Task CreateShortUrl_ShouldReturnCreatedAtActionResult_WhenRequestIsValid()
        {
            // Arrange
            var request = new CreateUrlMappingRequest
            {
                OriginalUrl = "https://ElectronicAndGames.com",
                CustomShortCode = "EAGA2025",
                Title = "Electronic and Games",
                Description = "Store for selling electronic devices and games",
                ExpiresAt = DateTime.UtcNow.AddDays(30)
            };

            var createdUrl = new UrlMapping
            {
                Id = 1,
                OriginalUrl = request.OriginalUrl,
                ShortCode = request.CustomShortCode,
                Title = request.Title,
                Description = request.Description,
                ExpiresAt = request.ExpiresAt,
                CreatedAt = DateTime.UtcNow
            };

            _serviceMock
                .Setup(s => s.CreateUrlMappingAsync(It.IsAny<UrlMapping>(), request.CustomShortCode))
                .ReturnsAsync(createdUrl);

            // Act
            var result = await _controller.CreateShortUrl(request);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            var response = Assert.IsType<CreateUrlMappingResponse>(createdResult.Value);

            Assert.Equal(request.CustomShortCode, response.ShortCode);
            Assert.Equal(1, response.Id);
            Assert.Equal($"https://ShortUrl/{request.CustomShortCode}", response.ShortUrl);
        }

        [Fact]
        public async Task DeleteUrlMapping_ShouldReturnNoContent_WhenUrlExists()
        {
            // Arrange
            int urlId = 1;
            var existingUrl = new UrlMapping { Id = urlId, ShortCode = "EAGA2025" };

            _serviceMock.Setup(s => s.GetByIdAsync(urlId)).ReturnsAsync(existingUrl);
            _serviceMock.Setup(s => s.DeleteUrlAsync(urlId)).Returns(Task.CompletedTask);

            // Act
            var result = await _controller.DeleteUrlMapping(urlId);

            // Assert
            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task DeleteUrlMapping_ShouldReturnNotFound_WhenUrlDoesNotExist()
        {
            // Arrange
            int urlId = 1;
            _serviceMock.Setup(s => s.GetByIdAsync(urlId)).ReturnsAsync((UrlMapping)null);

            // Act
            var result = await _controller.DeleteUrlMapping(urlId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal(404, notFoundResult.StatusCode);
        }

        [Fact]
        public async Task UpdateUrl_ShouldReturnOk_WhenUpdateIsSuccessful()
        {
            // Arrange
            var updateRequest = new UpdateUrlMappingRequest
            {
                Id = 1,
                OriginalUrl = "https://UpdatedUrl.com",
                Title = "Updated Title",
                Description = "Updated description",
                CustomShortCode = "EAGA2025",
                ExpiresAt = DateTime.UtcNow.AddDays(30)
            };

            var existingUrl = new UrlMapping { Id = 1, ShortCode = "EAGA2025" };

            _serviceMock.Setup(s => s.GetByIdAsync(updateRequest.Id)).ReturnsAsync(existingUrl);
            _serviceMock.Setup(s => s.UpdateUrlAsync(It.IsAny<UrlMapping>(), updateRequest.CustomShortCode))
                        .ReturnsAsync(existingUrl);

            // Act
            var result = await _controller.UpdateUrl(updateRequest);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result); // for ActionResult<T>
            var response = Assert.IsType<UrlMappingResponse>(okResult.Value);

            Assert.Equal(updateRequest.CustomShortCode, response.ShortCode);
        }

        [Fact]
        public async Task UpdateUrl_ShouldReturnNotFound_WhenUrlMappingDoesNotExist()
        {
            // Arrange
            var updateRequest = new UpdateUrlMappingRequest
            {
                Id = 999, // non-existent ID
                Title = "Attempted Update",
                Description = "This update should fail",
                CustomShortCode = "FAIL2025",
                OriginalUrl = "http://nonexistent.com",
                ExpiresAt = DateTime.UtcNow.AddDays(30)
            };

            // Mock the service to throw KeyNotFoundException when updating a non-existent URL mapping
            _serviceMock
                .Setup(s => s.UpdateUrlAsync(It.IsAny<UrlMapping>(), updateRequest.CustomShortCode))
                .ThrowsAsync(new KeyNotFoundException("UrlMapping not found."));

            // Act
            var result = await _controller.UpdateUrl(updateRequest);

            // Assert
            var objectResult = Assert.IsType<NotFoundObjectResult>(result.Result); // Controller returns ObjectResult on exception
            Assert.Equal(404, objectResult.StatusCode);
            Assert.Equal($"URL with ID {updateRequest.Id} not found.", objectResult.Value);
        }

        [Fact]
        public async Task GetAllUrls_ShouldReturnOk_WithAllUrls()
        {
            // Arrange
            var urls = new List<UrlMapping>
            {
                new UrlMapping { Id = 1, ShortCode = "abc123", OriginalUrl = "http://example1.com" },
                new UrlMapping { Id = 2, ShortCode = "def456", OriginalUrl = "http://example2.com" }
            };
            _serviceMock.Setup(s => s.GetAllUrlsAsync()).ReturnsAsync(urls);

            // Act
            var result = await _controller.GetAllUrls();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsAssignableFrom<IEnumerable<UrlMappingResponse>>(okResult.Value);
            Assert.Equal(2, response.Count());
        }

        [Fact]
        public async Task GetUrlById_ShouldReturnOk_WhenUrlExists()
        {
            // Arrange
            var url = new UrlMapping { Id = 1, ShortCode = "abc123", OriginalUrl = "http://example.com" };
            _serviceMock.Setup(s => s.GetByIdAsync(1)).ReturnsAsync(url);

            // Act
            var result = await _controller.GetUrlById(1);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<UrlMappingResponse>(okResult.Value);
            Assert.Equal("abc123", response.ShortCode);
        }

        [Fact]
        public async Task GetUrlById_ShouldReturnNotFound_WhenUrlDoesNotExist()
        {
            // Arrange
            _serviceMock.Setup(s => s.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((UrlMapping)null);

            // Act
            var result = await _controller.GetUrlById(999);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
            Assert.Equal(404, notFoundResult.StatusCode);
        }

        [Fact]
        public async Task GetActiveUrls_ShouldReturnOk_WithActiveUrls()
        {
            // Arrange
            var urls = new List<UrlMapping>
            {
                new UrlMapping { Id = 1, ShortCode = "active1", OriginalUrl = "http://active.com", IsActive = true },
            };
            _serviceMock.Setup(s => s.GetActiveUrlsAsync()).ReturnsAsync(urls);

            // Act
            var result = await _controller.GetActiveUrls();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsAssignableFrom<IEnumerable<UrlMappingResponse>>(okResult.Value);
            Assert.Single(response);
        }

        [Fact]
        public async Task GetMostClickedUrl_ShouldReturnOk_WithPopularUrls()
        {
            // Arrange
            var urls = new List<UrlMapping>
            {
                new UrlMapping { Id = 1, ShortCode = "most1", OriginalUrl = "http://popular.com", ClickCount = 10 },
                new UrlMapping { Id = 2, ShortCode = "most2", OriginalUrl = "http://popular2.com", ClickCount = 8 }
            };
            _serviceMock.Setup(s => s.GetMostClickedUrlsAsync(2)).ReturnsAsync(urls);

            // Act
            var result = await _controller.GetMostClickedUrl(2);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsAssignableFrom<IEnumerable<UrlMappingResponse>>(okResult.Value);
            Assert.Equal(2, response.Count());
        }

        [Fact]
        public async Task RedirectToOriginalUrl_ShouldReturnUrl_WhenShortCodeExists()
        {
            // Arrange
            _serviceMock.Setup(s => s.RedirectToOriginalUrlAsync("abc123")).ReturnsAsync("http://original.com");

            // Act
            var result = await _controller.RedirectToOriginalUrl("abc123");

            // Assert
            var okResult = Assert.IsType<ActionResult<string>>(result);
            Assert.Equal("http://original.com", okResult.Value);
        }

        [Fact]
        public async Task RedirectToOriginalUrl_ShouldReturnNotFound_WhenShortCodeDoesNotExist()
        {
            // Arrange
            _serviceMock.Setup(s => s.RedirectToOriginalUrlAsync("nonexistent")).ReturnsAsync(string.Empty);

            // Act
            var result = await _controller.RedirectToOriginalUrl("nonexistent");

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
            Assert.Equal("Short URL not found", notFoundResult.Value);
        }


        [Fact]
        public async Task DeactivateExpiredUrlsAsync_ShouldSetIsActiveFalse_ForExpiredUrls()
        {
            var mockRepo = new Mock<IUrlMappingRepository>();
            var mockUnitOfWork = new Mock<IUnitOfWork>();
            var mockLogger = new Mock<ILogger<UrlMappingService>>();
            var mockShortUrlService = new Mock<IShortUrlGeneratorService>();
            var mockRedis = new Mock<IConnectionMultiplexer>();
            mockRedis.Setup(x => x.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(Mock.Of<IDatabase>());

            var service = new UrlMappingService(
                mockRepo.Object,
                mockUnitOfWork.Object,
                mockLogger.Object,
                mockShortUrlService.Object,
                mockRedis.Object
            );

            //Arrange
            var expiredUrls = new List<UrlMapping>
            {
                new UrlMapping { Id = 1, IsActive = true, ExpiresAt = DateTime.UtcNow.AddDays(-1) },
                new UrlMapping { Id = 2, IsActive = true, ExpiresAt = DateTime.UtcNow.AddHours(-2) }
            };
            mockRepo.Setup(r => r.GetExpiredUrlsAsync()).ReturnsAsync(expiredUrls);
            mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
                  .ReturnsAsync(1);
            //Act
            await service.DeactivateExpiredUrlsAsync();
            // Assert
            Assert.All(expiredUrls, u => Assert.False(u.IsActive));
            mockRepo.Verify(r => r.UpdateAsync(It.IsAny<UrlMapping>()), Times.Exactly(expiredUrls.Count));
            mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);


        }
    }
}
