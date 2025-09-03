using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using Domain.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Application.Services
{
    public class SafeBrowsingService : ISafeBrowsingService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly ILogger<SafeBrowsingService> _logger;

        public SafeBrowsingService(HttpClient httpClient, IConfiguration config, ILogger<SafeBrowsingService> logger)
        {
            _httpClient = httpClient;
            _apiKey = config["SafeBrowsing:ApiKey"];
            _logger = logger;
        }

        public async Task<bool> IsUrlSafeAsync(string url)
        {
            var requestBody = new
            {
                client = new { clientId = "your-app-name", clientVersion = "1.0" },
                threatInfo = new
                {
                    threatTypes = new[] { "MALWARE", "SOCIAL_ENGINEERING", "UNWANTED_SOFTWARE", "POTENTIALLY_HARMFUL_APPLICATION" },
                    platformTypes = new[] { "ANY_PLATFORM" },
                    threatEntryTypes = new[] { "URL" },
                    threatEntries = new[] { new { url } }
                }
            };

            var response = await _httpClient.PostAsJsonAsync(
                $"https://safebrowsing.googleapis.com/v4/threatMatches:find?key={_apiKey}",
                requestBody);

            var content = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("Safe Browsing API response: {Response}", content);
            if (string.IsNullOrWhiteSpace(content)) // New robust check logic
                return true;
            try
            {
                using (var jsonDoc = JsonDocument.Parse(content))
                {
                    return !jsonDoc.RootElement.EnumerateObject().Any();
                }
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse Safe Browsing API response.");
                return false; // Or handle error as appropriate
            }
        }
    }

}
