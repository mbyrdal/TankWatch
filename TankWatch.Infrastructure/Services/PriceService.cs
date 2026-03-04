using TankWatch.Core.DTOs;
using TankWatch.Core.Entities;
using TankWatch.Core.Interfaces;

namespace TankWatch.Infrastructure.Services;

public class PriceService : IPriceService
{
    private readonly IPriceRepository _priceRepo;
    private readonly INotificationService _notificationService;

    public PriceService(IPriceRepository priceRepo, INotificationService notificationService)
    {
        _priceRepo = priceRepo;
        _notificationService = notificationService;
    }
    
    public async Task<IEnumerable<PriceDto>> GetNearbyPricesAsync(NearbyQuery query)
    {
        var prices = await _priceRepo.GetLatestPricesNearbyAsync(query.Latitude, query.Longitude, query.RadiusKm, query.FuelTypeId);

        return prices.Select(p => new PriceDto
        {
            GasStationId = p.GasStationId,
            StationName = p.GasStation.Name,
            FuelType = p.FuelType.Name,
            Amount = p.Amount,
            UpdatedAt = p.UpdatedAt,
            Latitude = p.GasStation.Latitude,
            Longitude = p.GasStation.Longitude
        });
    }
    
    public async Task ReportPriceAsync(int stationId, int fuelTypeId, decimal amount, string source)
    {
        var price = new Price
        {
            GasStationId = stationId,
            FuelTypeId = fuelTypeId,
            Amount = amount,
            UpdatedAt = DateTime.UtcNow,
            Source = source
        };

        var savedPrice = await _priceRepo.AddPriceAsync(price);

        // Notify subscribers via SignalR
        await _notificationService.NotifyPriceUpdate(stationId, savedPrice);
    }
}