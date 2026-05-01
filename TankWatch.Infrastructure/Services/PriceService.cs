using TankWatch.Core.DTOs;
using TankWatch.Core.Entities;
using TankWatch.Core.Interfaces;

namespace TankWatch.Infrastructure.Services;

public class PriceService : IPriceService
{
    private readonly IPriceRepository _priceRepo;

    public PriceService(IPriceRepository priceRepo)
    {
        _priceRepo = priceRepo;
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
    }
    
    public async Task<IEnumerable<PriceHistoryDto>> GetPriceHistoryAsync(int stationId, int fuelTypeId, int days)
    {
        return await _priceRepo.GetPriceHistoryAsync(stationId, fuelTypeId, days);
    }
    
    public async Task<IEnumerable<PriceHistoryDto>> GetBrandPriceHistoryAsync(string brand, int fuelTypeId, int days)
    {
        return await _priceRepo.GetBrandPriceHistoryAsync(brand, fuelTypeId, days);
    }
}