using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TankWatch.Core.Entities;
using TankWatch.Core.Interfaces;
using TankWatch.Infrastructure.Data;
using TankWatch.Infrastructure.Geocoding;

namespace TankWatch.Infrastructure.Services;

public class PriceScraperService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<PriceScraperService> _logger;
    private readonly NominatimGeocoder _geocoder;
    private readonly Q8FuelPriceProvider _q8Provider;

    public PriceScraperService(IServiceProvider services, ILogger<PriceScraperService> logger, NominatimGeocoder geocoder,  Q8FuelPriceProvider q8Provider)
    {
        _services = services;
        _logger = logger;
        _geocoder = geocoder;
        _q8Provider = q8Provider;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Call orchestrator method that runs all providers
                await ScrapeAllPricesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while scraping prices");
            }

            // Wait 1 hour before next run (adjust as needed)
            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }
    
    // Orchestrator method
    private async Task ScrapeAllPricesAsync(CancellationToken stoppingToken)
    {
        // await ScrapeCircleKPricesAsync(stoppingToken); DISABLED FOR NOW.... RATE LIMITING
        await ScrapeQ8PricesAsync(stoppingToken);
        // Add more providers here as needed
    }
    
    // Currently disabled because of CircleK API limitations... will still function though.
    private async Task ScrapeCircleKPricesAsync(CancellationToken stoppingToken) 
    {
        _logger.LogInformation("Starting CircleK price scrape...");

        using var scope = _services.CreateScope();
        var circleKProvider = scope.ServiceProvider.GetRequiredService<CircleKFuelPriceProvider>();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var priceRepo = scope.ServiceProvider.GetRequiredService<IPriceRepository>();

        // Fetch all sites with prices (rate‑limited internally)
        var siteDetails = await circleKProvider.FetchAllSitesWithPricesAsync();

        if (siteDetails.Count == 0)
        {
            _logger.LogWarning("No data received from Circle K");
            return;
        }

        foreach (var detail in siteDetails)
        {
            double latitude = 0;
            double longitude = 0;

            // Try to use coordinates from API if present
            if (detail.Latitude.HasValue && detail.Longitude.HasValue &&
                detail.Latitude.Value != 0 && detail.Longitude.Value != 0)
            {
                latitude = detail.Latitude.Value;
                longitude = detail.Longitude.Value;
                _logger.LogDebug("Using API-provided coordinates for station {StationId}", detail.Id);
            }
            // FALLBACK: Geocode the address
            else if (!string.IsNullOrWhiteSpace(detail.Address.Street))
            {
                var fullAddress = $"{detail.Address.Street}, {detail.Address.PostalCode} {detail.Address.City}, Denmark";
                var coords = await _geocoder.GeocodeAddressAsync(fullAddress);

                if (coords.HasValue)
                {
                    latitude = coords.Value.Latitude;
                    longitude = coords.Value.Longitude;
                    _logger.LogDebug("Geocoded station {StationId}: ({Lat}, {Lon})", detail.Id, latitude, longitude);
                }
                else
                {
                    _logger.LogWarning("Could not geocode station {StationId}, using fallback (0,0)", detail.Id);
                }

                // Wait 1 second ONLY if actually using geocode
                await Task.Delay(1000, stoppingToken);
            }
            else
            {
                _logger.LogWarning("Station {StationId} has no street address and no API coordinates", detail.Id);
            }

            // Find or create station using detail.Id as ExternalId
            var existingStation = context.GasStations
                .FirstOrDefault(s => s.ExternalId == detail.Id);

            if (existingStation == null)
            {
                existingStation = new GasStation
                {
                    ExternalId = detail.Id,
                    Name = detail.Name,
                    Brand = "Circle K",
                    Address = $"{detail.Address.Street}, {detail.Address.City} {detail.Address.PostalCode}",
                    City = detail.Address.City,
                    PostalCode = detail.Address.PostalCode,
                    Latitude = latitude,
                    Longitude = longitude,
                    IsActive = true,
                    LastUpdated = DateTime.UtcNow
                };
                context.GasStations.Add(existingStation);
                await context.SaveChangesAsync(stoppingToken);
            }
            else
            {
                existingStation.Name = detail.Name;
                existingStation.Address = $"{detail.Address.Street}, {detail.Address.City} {detail.Address.PostalCode}";
                existingStation.City = detail.Address.City;
                existingStation.PostalCode = detail.Address.PostalCode;
                existingStation.Latitude = latitude;
                existingStation.Longitude = longitude;
                existingStation.LastUpdated = DateTime.UtcNow;
                await context.SaveChangesAsync(stoppingToken);
            }

            // Map fuel types to IDs
            var fuelTypeMapping = new Dictionary<string, int>
            {
                ["Diesel"] = 1,
                ["95"] = 2,
                ["98"] = 3
            };

            foreach (var fp in detail.FuelPrices)
            {
                if (fuelTypeMapping.TryGetValue(fp.DisplayName, out int fuelTypeId))
                {
                    await UpdatePriceHistoryIfChanged(priceRepo, existingStation.Id, fuelTypeId, fp.Price, "CircleK");
                }
                else 
                {
                    _logger.LogWarning("Unknown fuel type '{DisplayName}' for station {StationId}", fp.DisplayName, detail.Id); 
                }
            }
        }

        _logger.LogInformation("Price scrape completed successfully.");
    }

    private async Task ScrapeQ8PricesAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting Q8/F24 price fetch...");
        
        using var scope = _services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var priceRepo = scope.ServiceProvider.GetRequiredService<IPriceRepository>();
        
        var apiResponse = await _q8Provider.FetchAllPricesAsync();
        
        if (apiResponse == null)
        {
            _logger.LogWarning("No data received from Q8/F24 (null response)");
            return;
        }
        
        if (apiResponse.Data.StationsPrices.Count == 0)
        {
            _logger.LogWarning("No data received from Q8/F24 (empty or missing data)");
            return;
        }
        
        // Group all product entries by stationId (each station appears multiple times, once per product)
        var groupedStations = apiResponse.Data.StationsPrices
            .GroupBy(sp => sp.StationId)
            .ToList();
        
        foreach (var group in groupedStations)
        {
            var firstEntry = group.First();
            var externalId = firstEntry.StationId;

            // Determine brand: if stationName starts with "F24", brand = "F24", else "Q8"
            var brand = firstEntry.StationName?.StartsWith("F24", StringComparison.OrdinalIgnoreCase) == true ? "F24" : "Q8";

            // Address may be null (especially for Q8 stations)
            var address = firstEntry.Address ?? string.Empty;

            // Find or create station in database
            var existingStation = context.GasStations.FirstOrDefault(s => s.ExternalId == externalId);
            if (existingStation == null)
            {
                existingStation = new GasStation
                {
                    ExternalId = externalId,
                    Name = firstEntry.StationName ?? $"Q8/F24 Station {externalId}",
                    Brand = brand,
                    Address = address,
                    City = string.Empty,      // Not provided by API
                    PostalCode = string.Empty,
                    Latitude = 0,              // No coordinates in this API
                    Longitude = 0,
                    IsActive = true,
                    LastUpdated = DateTime.UtcNow
                };
                context.GasStations.Add(existingStation);
                await context.SaveChangesAsync(stoppingToken);
            }
            else
            {
                // Update existing station details (name, address, brand) if they've changed
                existingStation.Name = firstEntry.StationName ?? existingStation.Name;
                existingStation.Address = address;
                existingStation.Brand = brand;
                existingStation.LastUpdated = DateTime.UtcNow;
                await context.SaveChangesAsync(stoppingToken);
            }

            // Collect all products for this station (flatten the group)
            var allProducts = group.SelectMany(g => g.Products).ToList();

            // Map product names to fuel type IDs (based on your FuelTypes table)
            // Adjust these mappings based on actual product names you want to track
            var fuelTypeMapping = new Dictionary<string, int>
            {
                ["GoEasy Diesel"] = 1,
                ["GoEasy Diesel Extra"] = 1,      // If you treat both as diesel
                ["GoEasy 95 E10"] = 2,
                ["GoEasy 95 Extra E5"] = 2,      // Also 95 octane
                // Add other fuel types as needed, e.g., "Neste MY (HVO100)" if you want to map to a specific type
            };

            foreach (var product in allProducts)
            {
                if (fuelTypeMapping.TryGetValue(product.ProductName, out int fuelTypeId))
                {
                    await UpdatePriceHistoryIfChanged(priceRepo, existingStation.Id, fuelTypeId, product.Price, brand);
                }
                else
                {
                    _logger.LogDebug("Ignored product {ProductName} for station {StationId}", product.ProductName, externalId);
                }
            }
        }
        
        _logger.LogInformation("Q8/F24 price fetch completed.");
    }
    
    private async Task UpdatePriceHistoryIfChanged(IPriceRepository priceRepo, int stationId, int fuelTypeId, decimal newPrice, string source)
    {
        // The MERGE command handles the "if changed" logic internally, no separate query needed.
        _logger.LogDebug("Updating price for station {StationId}, fuel {FuelTypeId} to {Price}", stationId, fuelTypeId, newPrice);
        await priceRepo.UpdatePriceHistoryAsync(stationId, fuelTypeId, newPrice, source);
    }
}