using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TankWatch.Infrastructure.Data;
using TankWatch.Infrastructure.Geocoding;

namespace TankWatch.Infrastructure.Services;

public class GeocodingBackgroundService  : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<GeocodingBackgroundService> _logger;

    public GeocodingBackgroundService(IServiceProvider services, ILogger<GeocodingBackgroundService> logger)
    {
        _services = services;
        _logger = logger;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await GeocodeMissingStationsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during geocoding job");
            }

            // Wait 24 hours before next run
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }
    }
    
    private async Task GeocodeMissingStationsAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting geocoding job for stations with missing coordinates...");

        using var scope = _services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var geocoder = scope.ServiceProvider.GetRequiredService<NominatimGeocoder>();

        // Find stations with latitude = 0 and a non-empty address
        var stationsToGeocode = await context.GasStations
            .Where(s => s.Latitude == 0 && s.Longitude == 0 && !string.IsNullOrWhiteSpace(s.Address))
            .ToListAsync(stoppingToken);

        _logger.LogInformation("Found {Count} stations to geocode", stationsToGeocode.Count);

        foreach (var station in stationsToGeocode)
        {
            // Use full address string.
            var coords = await geocoder.GeocodeAddressAsync(station.Address);

            if (coords.HasValue)
            {
                station.Latitude = coords.Value.Latitude;
                station.Longitude = coords.Value.Longitude;
                station.LastUpdated = DateTime.UtcNow;
                _logger.LogDebug("Geocoded station {StationId} ({Name}) to ({Lat}, {Lon})",
                    station.Id, station.Name, station.Latitude, station.Longitude);
            }
            else
            {
                _logger.LogWarning("Failed to geocode station {StationId}: {Address}", station.Id, station.Address);
            }

            await context.SaveChangesAsync(stoppingToken);

            // Respecting Nominatim's usage policy: 1 request per second
            await Task.Delay(1000, stoppingToken);
        }

        _logger.LogInformation("Geocoding job completed.");
    }
}