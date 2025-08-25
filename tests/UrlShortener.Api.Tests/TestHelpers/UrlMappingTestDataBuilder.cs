using System.Linq;
using Bogus;
using Domain.Entities;

namespace UrlShortener.Api.Tests.TestHelpers;

public class UrlMappingTestDataBuilder
{
    private readonly Faker<UrlMapping> _faker;

    public UrlMappingTestDataBuilder()
    {
        _faker = new Faker<UrlMapping>()
            .RuleFor(x => x.Id, f => f.Random.Number(1, 999999))
            .RuleFor(x => x.OriginalUrl, f => f.PickRandom(GetRealisticUrls()))
            .RuleFor(x => x.ShortCode, f => f.Random.String2(6, "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789"))
            .RuleFor(x => x.ClickCount, f => f.Random.Number(0, 1000))
            .RuleFor(x => x.IsActive, f => f.Random.Bool(0.9f))
            .RuleFor(x => x.ExpiresAt, f => f.Date.Future().OrNull(f, 0.3f))
            .RuleFor(x => x.CreatedAt, f => f.Date.Past())
            .RuleFor(x => x.UpdatedAt, f => f.Date.Recent());
    }

    public UrlMapping Build() => _faker.Generate();

    public List<UrlMapping> Build(int count) => _faker.Generate(count);

    public UrlMappingTestDataBuilder WithOriginalUrl(string originalUrl)
    {
        _faker.RuleFor(x => x.OriginalUrl, originalUrl);
        return this;
    }

    public UrlMappingTestDataBuilder WithShortCode(string shortCode)
    {
        _faker.RuleFor(x => x.ShortCode, shortCode);
        return this;
    }

    public UrlMappingTestDataBuilder WithClickCount(int clickCount)
    {
        _faker.RuleFor(x => x.ClickCount, clickCount);
        return this;
    }

    public UrlMappingTestDataBuilder WithExpiredDate()
    {
        _faker.RuleFor(x => x.ExpiresAt, f => f.Date.Past());
        return this;
    }

    public UrlMappingTestDataBuilder WithFutureExpiry()
    {
        _faker.RuleFor(x => x.ExpiresAt, f => f.Date.Future());
        return this;
    }

    public UrlMappingTestDataBuilder WithNoExpiry()
    {
        _faker.RuleFor(x => x.ExpiresAt, (DateTime?)null);
        return this;
    }

    public UrlMappingTestDataBuilder AsActive()
    {
        _faker.RuleFor(x => x.IsActive, true);
        return this;
    }

    public UrlMappingTestDataBuilder AsInactive()
    {
        _faker.RuleFor(x => x.IsActive, false);
        return this;
    }

    private static string[] GetRealisticUrls()
    {
        return new[]
        {
            "https://github.com/microsoft/dotnet",
            "https://docs.microsoft.com/en-us/aspnet/core/",
            "https://www.nuget.org/packages/Microsoft.AspNetCore.App/",
            "https://stackoverflow.com/questions/tagged/asp.net-core",
            "https://www.youtube.com/watch?v=dQw4w9WgXcQ",
            "https://medium.com/@author/building-scalable-apis-with-dotnet",
            "https://dev.to/community/understanding-clean-architecture",
            "https://www.linkedin.com/posts/activity-123456789",
            "https://twitter.com/dotnet/status/123456789",
            "https://news.ycombinator.com/item?id=12345678",
            "https://reddit.com/r/programming/comments/abc123",
            "https://shop.example.com/products/laptop-gaming?ref=homepage",
            "https://blog.example.com/2024/technology-trends",
            "https://conference.example.com/sessions/api-design-best-practices",
            "https://app.example.com/dashboard?utm_source=email&utm_medium=newsletter"
        };
    }
}