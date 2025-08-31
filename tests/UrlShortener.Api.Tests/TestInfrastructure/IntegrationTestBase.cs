using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Infrastructure.Data;
using StackExchange.Redis;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace UrlShortener.Api.Tests.TestInfrastructure;

public abstract class IntegrationTestBase : IClassFixture<TestWebApplicationFactory>, IAsyncLifetime
{
    protected readonly TestWebApplicationFactory Factory;
    protected readonly HttpClient HttpClient;
    protected ApplicationDbContext DbContext => _serviceScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    protected IDatabase RedisDatabase => _serviceScope.ServiceProvider.GetRequiredService<IConnectionMultiplexer>().GetDatabase();

    private readonly IServiceScope _serviceScope;

    protected IntegrationTestBase(TestWebApplicationFactory factory)
    {
        Factory = factory;
        // Create HttpClient that does NOT automatically follow redirects
        // This allows us to test 302 redirect responses directly
        HttpClient = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        _serviceScope = factory.Services.CreateScope();
    }

    public virtual async Task InitializeAsync()
    {
        // Clear at start of test to ensure clean state
        await ClearDatabaseAsync();
        await ClearCacheAsync();
    }

    public virtual async Task DisposeAsync()
    {
        // Clean up after test completes
        _serviceScope.Dispose();
    }

    protected async Task ClearDatabaseAsync()
    {
        await DbContext.UrlMappings.ExecuteDeleteAsync();
        await DbContext.SaveChangesAsync();
    }

    protected async Task ClearCacheAsync()
    {
        try
        {
            // Try admin command first
            await RedisDatabase.ExecuteAsync("FLUSHDB");
        }
        catch
        {
            // Fallback: manually delete keys with known prefixes
            var server = RedisDatabase.Multiplexer.GetServer(RedisDatabase.Multiplexer.GetEndPoints().First());
            try
            {
                var keys = server.Keys(pattern: "redirect:*").Concat(server.Keys(pattern: "entity:*"));
                foreach (var key in keys)
                {
                    await RedisDatabase.KeyDeleteAsync(key);
                }
            }
            catch
            {
                // If we can't clear cache, continue - tests should still work
            }
        }
    }

    protected async Task<HttpResponseMessage> PostJsonAsync<T>(string requestUri, T content)
    {
        var json = JsonSerializer.Serialize(content);
        var stringContent = new StringContent(json, Encoding.UTF8, "application/json");
        return await HttpClient.PostAsync(requestUri, stringContent);
    }

    protected async Task<T?> DeserializeAsync<T>(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
    }

    protected void AssertSuccessResponse(HttpResponseMessage response)
    {
        response.IsSuccessStatusCode.Should().BeTrue(
            because: $"Expected success status code but got {response.StatusCode}. Content: {response.Content.ReadAsStringAsync().Result}");
    }

    protected async Task EnsureTransactionCommitAsync()
    {
        // Force transaction commit and clear entity tracking
        await DbContext.SaveChangesAsync();
        DbContext.ChangeTracker.Clear();
        
        // Small delay to ensure database consistency
        await Task.Delay(100);
    }
}