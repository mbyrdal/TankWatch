using System.Text.Json;
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
     
     public async Task<(double Latitude, double Longitude)?> GeocodeAddressAsync(string fullAddress)
     {
         try
         {
             var url = $"search?q={Uri.EscapeDataString(fullAddress)}&format=json&limit=1";
             _logger.LogInformation("Geocoding request: {Url}", url);
 
             var response = await _httpClient.GetAsync(url);
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
             if (first != null && double.TryParse(first.Lat, out var lat) && double.TryParse(first.Lon, out var lon))
             {
                 return (lat, lon);
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
 }