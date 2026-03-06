using System.Text.Json;
using System.Text.RegularExpressions;
using System.Globalization;
using Microsoft.Extensions.Logging;

namespace TankWatch.Infrastructure.Geocoding;

public class NominatimGeocoder
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<NominatimGeocoder> _logger;

    public NominatimGeocoder(HttpClient httpClient, ILogger<NominatimGeocoder> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    private class NominatimResult
    {
        public string Lat { get; set; } = string.Empty;
        public string Lon { get; set; } = string.Empty;
    }

    // List of common Danish directional suffixes to remove
    private static readonly string[] DirectionalSuffixes = { "nord", "syd", "øst", "vest", "n", "s", "ø", "v" };

    public async Task<(double Latitude, double Longitude)?> GeocodeAddressAsync(string fullAddress, string? brand = null)
    {
        try
        {
            // 1. Basic cleaning
            var cleaned = fullAddress
                .Replace("Danmark", "")
                .Replace("v/", " ")          // "v/" often means "ved" – remove or replace with space
                .Trim();

            // 2. Remove house number ranges (e.g., "2-8" -> "2")
            cleaned = Regex.Replace(cleaned, @"(\d+)-(\d+)", "$1");

            // 3. Always try with brand + cleaned address first (if brand provided)
            if (!string.IsNullOrWhiteSpace(brand))
            {
                var brandedQuery = $"{brand} {cleaned}";
                var coords = await TryGeocodeAsync(brandedQuery);
                if (coords.HasValue) return coords;
            }

            // 4. Try just the cleaned address
            var coords2 = await TryGeocodeAsync(cleaned);
            if (coords2.HasValue) return coords2;

            // 5. Fallback: brand + street name only (without house number)
            if (!string.IsNullOrWhiteSpace(brand))
            {
                var streetPart = ExtractStreetName(cleaned);
                if (!string.IsNullOrWhiteSpace(streetPart))
                {
                    var fallback = $"{brand} {streetPart}";
                    return await TryGeocodeAsync(fallback);
                }
            }

            _logger.LogWarning("No geocoding result for address: {Address}", fullAddress);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Geocoding failed for address: {Address}", fullAddress);
            return null;
        }
    }

    // Helper to extract the core street name by removing postal code and everything after it
    private string ExtractStreetName(string address)
    {
        // Take everything before the first digit that is part of a house number
        var match = Regex.Match(address, @"^(.*?)\s+\d");
        if (match.Success) return match.Groups[1].Value.Trim();
        return address.Split(',')[0].Trim();
    }

    private async Task<(double Latitude, double Longitude)?> TryGeocodeAsync(string query)
    {
        var url = $"search?q={Uri.EscapeDataString(query)}&format=json&limit=1";
        _logger.LogInformation("Geocoding request: {Url}", url);

        using var response = await _httpClient.GetAsync(url);
        var content = await response.Content.ReadAsStringAsync();

        // Check for Nominatim rate-limiting
        if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
        {
            throw new HttpRequestException("429 Too Many Requests");
        }

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Geocoding failed with status {StatusCode}. Response: {Content}",
                response.StatusCode, content);
            return null;
        }

        var results = JsonSerializer.Deserialize<List<NominatimResult>>(content,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        var first = results?.FirstOrDefault();
        if (first != null &&
            double.TryParse(first.Lat, NumberStyles.Any, CultureInfo.InvariantCulture, out var lat) &&
            double.TryParse(first.Lon, NumberStyles.Any, CultureInfo.InvariantCulture, out var lon))
        {
            return (lat, lon);
        }

        return null;
    }
}