using System.Text.Json.Serialization;

namespace TankWatch.Core.Models;

// Response from GET /v1/prices/fuel/countries/DK
public class CircleKSiteListResponse
{
    [JsonPropertyName("sites")]
    public List<CircleKSiteSummary> Sites { get; set; } = new();
}

public class CircleKSiteSummary
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("phoneNumber")]
    public string? PhoneNumber { get; set; }

    [JsonPropertyName("address")]
    public CircleKAddress Address { get; set; } = new();
}

public class CircleKAddress
{
    [JsonPropertyName("street")]
    public string Street { get; set; } = string.Empty;

    [JsonPropertyName("city")]
    public string City { get; set; } = string.Empty;

    [JsonPropertyName("state")]
    public string? State { get; set; }

    [JsonPropertyName("postalCode")]
    public string PostalCode { get; set; } = string.Empty;

    [JsonPropertyName("country")]
    public string Country { get; set; } = string.Empty;
}

// Response from GET /v1/prices/fuel/sites/{siteId}
public class CircleKSiteDetailResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("phoneNumber")]
    public string? PhoneNumber { get; set; }
    
    // Optional coordinates (if the API ever includes them)
    [JsonPropertyName("latitude")]
    public double? Latitude { get; set; }

    // Optional coordinates (if the API ever includes them)
    [JsonPropertyName("longitude")]
    public double? Longitude { get; set; }

    [JsonPropertyName("address")]
    public CircleKAddress Address { get; set; } = new();

    [JsonPropertyName("fuelPrices")]
    public List<CircleKFuelPrice> FuelPrices { get; set; } = new();
}

public class CircleKFuelPrice
{
    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = string.Empty;  // e.g., "Diesel", "95", "98"

    [JsonPropertyName("price")]
    public decimal Price { get; set; }

    [JsonPropertyName("currency")]
    public string Currency { get; set; } = string.Empty;

    [JsonPropertyName("volumeUnit")]
    public string VolumeUnit { get; set; } = string.Empty;

    [JsonPropertyName("lastUpdated")]
    public DateTime LastUpdated { get; set; }
}