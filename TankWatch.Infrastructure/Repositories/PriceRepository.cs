using System.Globalization;
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

    /// <summary>
    /// Retrieves the latest fuel prices for gas stations within a specified radius of a location.
    /// Uses PostGIS spatial indexing to quickly filter stations by distance, and returns the most recent price per station and fuel type.
    /// </summary>
    /// <param name="lat">Latitude of the center point (WGS84).</param>
    /// <param name="lon">Longitude of the center point (WGS84).</param>
    /// <param name="radiusKm">Search radius in kilometers.</param>
    /// <param name="fuelTypeId">Optional fuel type ID to filter results.</param>
    /// <returns>A collection of Price entities with eager-loaded GasStation and FuelType navigation properties.</returns>
    public async Task<IEnumerable<Price>> GetLatestPricesNearbyAsync(double lat, double lon, double radiusKm, int? fuelTypeId = null)
    {
        // Build the point as WKT (longitude first); use invariant culture to ensure decimal point
        var pointWkt = $"POINT({lon.ToString(CultureInfo.InvariantCulture)} {lat.ToString(CultureInfo.InvariantCulture)})";
        
        // Base SQL with DISTINCT ON to get the latest price per station and fuel type
        var sql = @"
            SELECT DISTINCT ON (p.""GasStationId"", p.""FuelTypeId"") 
                p.*
            FROM ""Prices"" p
            INNER JOIN ""GasStations"" gs ON p.""GasStationId"" = gs.""Id""
            WHERE gs.""IsActive"" = true
              AND ST_DWithin(gs.""Location"", ST_GeogFromText({0}), {1})
        ";
        
        // Add fuel type filter if needed
        if (fuelTypeId.HasValue)
        {
            sql += " AND p.\"FuelTypeId\" = {2}";
        }
        
        sql += " ORDER BY p.\"GasStationId\", p.\"FuelTypeId\", p.\"UpdatedAt\" DESC";
        
        // Prepare parameters
        var parameters = fuelTypeId.HasValue
            ? new object[] { pointWkt, radiusKm * 1000, fuelTypeId.Value }
            : new object[] { pointWkt, radiusKm * 1000 };
        
        // Execute raw SQL and materialize the Price entities
        var prices = await _context.Prices
            .FromSqlRaw(sql, parameters)
            .Include(p => p.GasStation)
            .Include(p => p.FuelType)
            .ToListAsync();

        return prices;
    }

    public async Task<Price> AddPriceAsync(Price price)
    {
        _context.Prices.Add(price);
        await _context.SaveChangesAsync();
        return price;
    }
}