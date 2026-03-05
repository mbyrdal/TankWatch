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
            // 1. Clean hyphenated number ranges: replace "2-8" with "2"
            var cleanedAddress = Regex.Replace(fullAddress, @"(\d+)-(\d+)", "$1");

            // 2. Apply known street name corrections
            var corrections = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "Hovedgaden", "Hovedgade" },
                // Add other mismatches here as you find them
            };

            foreach (var (original, corrected) in corrections)
            {
                if (cleanedAddress.Contains(original, StringComparison.OrdinalIgnoreCase))
                {
                    cleanedAddress = cleanedAddress.Replace(original, corrected, StringComparison.OrdinalIgnoreCase);
                    break;
                }
            }

            // 3. Remove directional suffixes that are separate words
            var suffixPattern = @"\s+(" + string.Join("|", DirectionalSuffixes) + @")(?=\s|$)";
            cleanedAddress = Regex.Replace(cleanedAddress, suffixPattern, "", RegexOptions.IgnoreCase);
            cleanedAddress = Regex.Replace(cleanedAddress, @"\s+", " ").Trim();

            // 4. First attempt with cleaned address
            var result = await TryGeocodeAsync(cleanedAddress, fullAddress);
            if (result.HasValue)
                return result;

            // 5. Fallback: try with brand + street name + number (if brand is provided)
            if (!string.IsNullOrWhiteSpace(brand))
            {
                // Extract street part (everything before the comma, if present)
                var streetPart = fullAddress.Split(',')[0].Trim();
                // Remove postal code and city if they appear in the street part (unlikely)
                var fallbackQuery = $"{brand} {streetPart}";
                _logger.LogInformation("Fallback geocoding with brand: {Fallback}", fallbackQuery);
                result = await TryGeocodeAsync(fallbackQuery, fullAddress);
                if (result.HasValue)
                    return result;
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

    private async Task<(double Latitude, double Longitude)?> TryGeocodeAsync(string query, string originalAddress)
    {
        var url = $"search?q={Uri.EscapeDataString(query)}&format=json&limit=1";
        _logger.LogInformation("Geocoding request: {Url}", url);

        using var response = await _httpClient.GetAsync(url);
        var content = await response.Content.ReadAsStringAsync();

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