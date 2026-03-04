using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using TankWatch.Core.Models;

namespace TankWatch.Infrastructure.Services;

public class CircleKFuelPriceProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CircleKFuelPriceProvider> _logger;
    
    public CircleKFuelPriceProvider(HttpClient httpClient, ILogger<CircleKFuelPriceProvider> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }
    
    public async Task<List<CircleKSiteDetailResponse>> FetchAllSitesWithPricesAsync()
    {
        var sites = await FetchSiteListAsync();
        if (sites == null || sites.Count == 0)
            return new List<CircleKSiteDetailResponse>();

        var siteDetails = new List<CircleKSiteDetailResponse>();
        foreach (var site in sites)
        {
            try
            {
                _logger.LogInformation("Before delay for site {SiteId} at {Time}", site.Id, DateTime.Now.TimeOfDay);
                
                // Rate limiting: 1 request every 10 seconds
                await Task.Delay(1100);
                
                _logger.LogInformation("After delay for site {SiteId} at {Time}", site.Id, DateTime.Now.TimeOfDay);

                var detail = await FetchSiteDetailsAsync(site.Id);
                if (detail != null)
                    siteDetails.Add(detail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch details for site {SiteId}", site.Id);
            }
        }
        return siteDetails;
    }
    
    private async Task<List<CircleKSiteSummary>?> FetchSiteListAsync()
    {
        try
        {
            using var response = await _httpClient.GetAsync("v1/fuel/countries/DK");
            response.EnsureSuccessStatusCode();
        
            var content = await response.Content.ReadAsStringAsync();
            _logger.LogDebug("Raw response from Circle K site list: {Response}", content);
        
            var result = JsonSerializer.Deserialize<CircleKSiteListResponse>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            return result?.Sites;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching site list from Circle K");
            return null;
        }
    }
    
    private async Task<CircleKSiteDetailResponse?> FetchSiteDetailsAsync(string siteId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"v1/fuel/sites/{siteId}");
        
            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                // 1. Try Retry-After header (in seconds)
                var retryAfter = response.Headers.RetryAfter?.Delta?.TotalSeconds;
            
                if (retryAfter == null)
                {
                    // 2. If no header, try to read the response body (your JSON message)
                    var errorBody = await response.Content.ReadFromJsonAsync<RateLimitError>();
                    retryAfter = errorBody?.RetryAfterSeconds ?? 30; // fallback to 30
                }

                _logger.LogWarning("Rate limited for site {SiteId}. Waiting {RetryAfter}s", siteId, retryAfter);
                await Task.Delay(TimeSpan.FromSeconds(retryAfter.Value));
            
                return null; // Skip this site this time (or you could retry recursively, but careful)
            }
        
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<CircleKSiteDetailResponse>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching details for site {SiteId}", siteId);
            return null;
        }
    }
}

// Helper class to match the error JSON you received
public class RateLimitError
{
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    public double? RetryAfterSeconds
    {
        get
        {
            // Extract number from message like "Try again in 22 seconds."
            var match = System.Text.RegularExpressions.Regex.Match(Message, @"(\d+)");
            if (match.Success && int.TryParse(match.Groups[1].Value, out int seconds))
                return seconds;
            return null;
        }
    }
}