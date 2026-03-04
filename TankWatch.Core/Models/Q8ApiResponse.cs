using System.Text.Json.Serialization;

namespace TankWatch.Core.Models;

public class Q8ApiResponse
{
    [JsonPropertyName("data")]
    public Q8Data Data { get; set; } = new();
}

public class Q8Data
{
    [JsonPropertyName("stationsPrices")]
    public List<Q8StationProduct> StationsPrices { get; set; } = new();
}

// Represents one product entry for a station (the station may appear multiple times)
public class Q8StationProduct
{
    [JsonPropertyName("stationId")]
    public string StationId { get; set; } = string.Empty;

    [JsonPropertyName("stationName")]
    public string? StationName { get; set; }  // Can be null for Q8 stations

    [JsonPropertyName("address")]
    public string? Address { get; set; }       // Full address string, can be null

    [JsonPropertyName("products")]
    public List<Q8Product> Products { get; set; } = new(); // Note: this is an array inside each entry
}

public class Q8Product
{
    [JsonPropertyName("productId")]
    public string ProductId { get; set; } = string.Empty;

    [JsonPropertyName("productName")]
    public string ProductName { get; set; } = string.Empty;

    [JsonPropertyName("price")]
    public decimal Price { get; set; }

    [JsonPropertyName("unit")]
    public string Unit { get; set; } = string.Empty;

    [JsonPropertyName("priceChangeDate")]
    public DateTime PriceChangeDate { get; set; }
}