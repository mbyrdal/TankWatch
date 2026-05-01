using TankWatch.Core.DTOs;
using TankWatch.Core.Entities;

namespace TankWatch.Core.Interfaces;

public interface IPriceRepository
{
    Task<IEnumerable<Price>> GetLatestPricesForStationAsync(int stationId);
    Task<IEnumerable<Price>> GetLatestPricesNearbyAsync(double lat, double lon, double radiusKm, int? fuelTypeId = null);
    Task<Price> AddPriceAsync(Price price);
    Task UpdatePriceHistoryAsync(int gasStationId, int fuelTypeId, decimal amount, string source);
    Task<IEnumerable<PriceHistoryDto>> GetPriceHistoryAsync(int stationId, int fuelTypeId, int days);
    Task<IEnumerable<PriceHistoryDto>> GetBrandPriceHistoryAsync(string brand, int fuelTypeId, int days);
}