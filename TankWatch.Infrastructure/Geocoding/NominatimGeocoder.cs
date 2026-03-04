using System.Net.Http.Json;
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
    
    public async Task<(double Latitude, double Longitude)?> GeocodeAddressAsync(
        string street, string city, string postalCode, string country = "Denmark")
    {
        try
        {
            // Build a full address string
            var address = $"{street}, {postalCode} {city}, {country}";
            var url = $"search?q={Uri.EscapeDataString(address)}&format=json&limit=1";

            var response = await _httpClient.GetFromJsonAsync<List<NominatimResult>>(url);
            var first = response?.FirstOrDefault();

            if (first != null && double.TryParse(first.Lat, out var lat) && double.TryParse(first.Lon, out var lon))
            {
                return (lat, lon);
            }

            _logger.LogWarning("No geocoding result for address: {Address}", address);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Geocoding failed for address: {Street}, {PostalCode} {City}", street, postalCode, city);
            return null;
        }
    }
    
    private class NominatimResult
    {
        public string Lat { get; set; } = string.Empty;
        public string Lon { get; set; } = string.Empty;
    }
}