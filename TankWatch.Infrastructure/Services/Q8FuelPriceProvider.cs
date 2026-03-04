using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using TankWatch.Core.Models;

namespace TankWatch.Infrastructure.Services;

public class Q8FuelPriceProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<Q8FuelPriceProvider> _logger;
    
    public Q8FuelPriceProvider(HttpClient httpClient, ILogger<Q8FuelPriceProvider> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }
    
    public async Task<Q8ApiResponse?> FetchAllPricesAsync()
    {
        try
        {
            var url = "Station/GetStationPrices?page=1&pageSize=2000";
            var response = await _httpClient.GetFromJsonAsync<Q8ApiResponse>(url);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching prices from Q8/F24 API");
            return null;
        }
    }
}