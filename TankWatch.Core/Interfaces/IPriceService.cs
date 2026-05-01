using TankWatch.Core.DTOs;

namespace TankWatch.Core.Interfaces;

public interface IPriceService
{
    Task<IEnumerable<PriceDto>> GetNearbyPricesAsync(NearbyQuery query);
    Task ReportPriceAsync(int stationId, int fuelTypeId, decimal amount, string source);
    Task<IEnumerable<PriceHistoryDto>> GetPriceHistoryAsync(int stationId, int fuelTypeId, int days);
    Task<IEnumerable<PriceHistoryDto>> GetBrandPriceHistoryAsync(string brand, int fuelTypeId, int days);
}