using Microsoft.EntityFrameworkCore;
using TankWatch.Core.Entities;
using TankWatch.Core.Interfaces;
using TankWatch.Infrastructure.Data;

namespace TankWatch.Infrastructure.Repositories;

public class PriceRepository : IPriceRepository
{
    private readonly AppDbContext _context;

    public PriceRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Price>> GetLatestPricesForStationAsync(int stationId)
    {
        // For each fuel type, get the latest price
        var latestPrices = await _context.Prices
            .Where(p => p.GasStationId == stationId)
            .Include(p => p.FuelType)
            .GroupBy(p => p.FuelTypeId)
            .Select(g => g.OrderByDescending(p => p.UpdatedAt).First())
            .ToListAsync();
        return latestPrices;
    }
    
    public async Task<IEnumerable<Price>> GetLatestPricesNearbyAsync(double lat, double lon, double radiusKm, int? fuelTypeId = null)
    {
        // For now, we'll return a simplified version – we'll improve with spatial SQL later.
        // This naive version gets all prices and filters in memory (not for production).
        // Better approach: use raw SQL with PostGIS.
        var query = _context.Prices
            .Include(p => p.GasStation)
            .Include(p => p.FuelType)
            .Where(p => p.GasStation.IsActive);

        if (fuelTypeId.HasValue)
            query = query.Where(p => p.FuelTypeId == fuelTypeId.Value);

        var allPrices = await query.ToListAsync();

        // Filter by distance using Haversine formula (approximate)
        var nearby = allPrices
            .Where(p => CalculateDistance(lat, lon, p.GasStation.Latitude, p.GasStation.Longitude) <= radiusKm)
            .GroupBy(p => new { p.GasStationId, p.FuelTypeId })
            .Select(g => g.OrderByDescending(p => p.UpdatedAt).First())
            .ToList();

        return nearby;
    }
    
    private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        // Haversine formula
        const double r = 6371; // km
        var dLat = DegreesToRadians(lat2 - lat1);
        var dLon = DegreesToRadians(lon2 - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(DegreesToRadians(lat1)) * Math.Cos(DegreesToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return r * c;
    }

    private double DegreesToRadians(double degrees) => degrees * Math.PI / 180;

    public async Task<Price> AddPriceAsync(Price price)
    {
        _context.Prices.Add(price);
        await _context.SaveChangesAsync();
        return price;
    }
}