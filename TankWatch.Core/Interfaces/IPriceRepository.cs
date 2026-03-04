using TankWatch.Core.Entities;

namespace TankWatch.Core.Interfaces;

public interface IPriceRepository
{
    Task<IEnumerable<Price>> GetLatestPricesForStationAsync(int stationId);
    Task<IEnumerable<Price>> GetLatestPricesNearbyAsync(double lat, double lon, double radiusKm, int? fuelTypeId = null);
    Task<Price> AddPriceAsync(Price price);
}