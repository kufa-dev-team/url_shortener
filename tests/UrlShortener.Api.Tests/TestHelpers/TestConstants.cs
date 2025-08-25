namespace UrlShortener.Api.Tests.TestHelpers;

public static class TestConstants
{
    public static class Urls
    {
        public const string GitHub = "https://github.com";
        public const string Google = "https://www.google.com";
        public const string YouTube = "https://www.youtube.com/watch?v=dQw4w9WgXcQ";
        public const string LinkedIn = "https://www.linkedin.com/in/johndoe";
        public const string ECommerce = "https://shop.example.com/products/laptop-gaming?ref=homepage";
        public const string NewsArticle = "https://news.example.com/technology/ai-breakthrough-2024";
        public const string SocialMedia = "https://twitter.com/user/status/123456789";
        public const string Documentation = "https://docs.microsoft.com/en-us/aspnet/core/";
    }

    public static class ShortCodes
    {
        public const string Custom1 = "github01";
        public const string Custom2 = "docs1234";
        public const string Custom3 = "shop5678";
        public const string Custom4 = "news9876";
        public const string Simple = "abc12345";
        public const string WithNumbers = "test1234";
        public const string LowerCase = "mylink01";
        public const string MixedCase = "MyLink01";
    }

    public static class ApiEndpoints
    {
        public const string Shorten = "/api/shorten";
        public const string Redirect = "/api/redirect";
        public const string Analytics = "/api/analytics";
    }
}