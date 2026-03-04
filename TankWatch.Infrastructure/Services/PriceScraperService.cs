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

    public PriceScraperService(IServiceProvider services, ILogger<PriceScraperService> logger, NominatimGeocoder geocoder)
    {
        _services = services;
        _logger = logger;
        _geocoder = geocoder;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ScrapePricesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while scraping prices");
            }

            // Wait 1 hour before next run (adjust as needed)
            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }
    
    private async Task ScrapePricesAsync(CancellationToken stoppingToken) 
    {
        _logger.LogInformation("Starting price scrape...");

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
                var coords = await _geocoder.GeocodeAddressAsync(
                    detail.Address.Street,
                    detail.Address.City,
                    detail.Address.PostalCode);

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
                    await AddPriceIfChanged(priceRepo, existingStation.Id, fuelTypeId, fp.Price, "CircleK");
                }
                else 
                {
                    _logger.LogWarning("Unknown fuel type '{DisplayName}' for station {StationId}", fp.DisplayName, detail.Id); 
                }
            }
        }

        _logger.LogInformation("Price scrape completed successfully.");
    }
    
    private async Task AddPriceIfChanged(IPriceRepository priceRepo, int stationId, int fuelTypeId, decimal newPrice, string source)
    {
        var latestPrices = await priceRepo.GetLatestPricesForStationAsync(stationId);
        var latest = latestPrices.FirstOrDefault(p => p.FuelTypeId == fuelTypeId);

        if (latest == null || latest.Amount != newPrice)
        {
            await priceRepo.AddPriceAsync(new Price
            {
                GasStationId = stationId,
                FuelTypeId = fuelTypeId,
                Amount = newPrice,
                UpdatedAt = DateTime.UtcNow,
                Source = source
            });
        }
    }
}