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

            // Wait 6 hours before next run
            await Task.Delay(TimeSpan.FromDays(7), stoppingToken);
        }
    }
    
    private async Task GeocodeMissingStationsAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting geocoding job...");

        using var scope = _services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var geocoder = scope.ServiceProvider.GetRequiredService<NominatimGeocoder>();

        // Only stations with missing coordinates, address, and not attempted recently (7 days) and less than 5 attempts
        var cutoff = DateTime.UtcNow.AddDays(-7);
        var stationsToGeocode = await context.GasStations
            .Where(s => s.Latitude == 0 && s.Longitude == 0 
                        && !string.IsNullOrWhiteSpace(s.Address)
                        && (s.LastGeocodeAttempt == null || s.LastGeocodeAttempt < cutoff)
                        && s.GeocodeAttempts < 5)
            .OrderBy(s => s.LastGeocodeAttempt) // oldest first
            .Take(50) // max per run
            .ToListAsync(stoppingToken);

        _logger.LogInformation("Found {Count} stations to geocode this run", stationsToGeocode.Count);

        foreach (var station in stationsToGeocode)
        {
            station.LastGeocodeAttempt = DateTime.UtcNow;
            station.GeocodeAttempts++;

            try
            {
                var coords = await geocoder.GeocodeAddressAsync(station.Address, station.Brand);

                if (coords.HasValue)
                {
                    station.Latitude = coords.Value.Latitude;
                    station.Longitude = coords.Value.Longitude;
                    station.LastUpdated = DateTime.UtcNow;
                    _logger.LogDebug("Geocoded station {StationId}", station.Id);
                }
                else
                {
                    _logger.LogWarning("Failed to geocode station {StationId}", station.Id);
                }
            }
            catch (HttpRequestException ex) when (ex.Message.Contains("429"))
            {
                _logger.LogWarning("Rate limited (429) for station {StationId}. Stopping this run.", station.Id);
                await context.SaveChangesAsync(stoppingToken);
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error geocoding station {StationId}", station.Id);
            }

            await context.SaveChangesAsync(stoppingToken);
            await Task.Delay(2000, stoppingToken); // 2 seconds between stations
        }

        _logger.LogInformation("Geocoding job completed (processed {Count} stations).", stationsToGeocode.Count);
    }
}