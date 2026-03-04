using TankWatch.Core.DTOs;

namespace TankWatch.Core.Interfaces;

public interface IPriceService
{
    Task<IEnumerable<PriceDto>> GetNearbyPricesAsync(NearbyQuery query);
    Task ReportPriceAsync(int stationId, int fuelTypeId, decimal amount, string source);
}