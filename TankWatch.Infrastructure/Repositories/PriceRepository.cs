using System.Globalization;
using Microsoft.EntityFrameworkCore;
using TankWatch.Core.DTOs;
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
        var sql = @"
        SELECT DISTINCT ON (p.""FuelTypeId"") p.*
        FROM ""Prices"" p
        WHERE p.""GasStationId"" = {0}
        ORDER BY p.""FuelTypeId"", p.""UpdatedAt"" DESC";
        
        var prices = await _context.Prices
            .FromSqlRaw(sql, stationId)
            .Include(p => p.FuelType)
            .ToListAsync();
        return prices;
    }

    public async Task<IEnumerable<Price>> GetLatestPricesForStationOptimizedAsync(int stationId)
    {
        var sql = @"
        SELECT DISTINCT ON (p.""FuelTypeId"") p.*
        FROM ""Prices"" p
        WHERE p.""GasStationId"" = {0}
        ORDER BY p.""FuelTypeId"", p.""UpdatedAt"" DESC
    ";
        var prices = await _context.Prices
            .FromSqlRaw(sql, stationId)
            .Include(p => p.FuelType)
            .ToListAsync();
        return prices;
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
        var pointWkt = $"POINT({lon.ToString(CultureInfo.InvariantCulture)} {lat.ToString(CultureInfo.InvariantCulture)})";

        var sql = @"
        SELECT DISTINCT ON (p.""GasStationId"", p.""FuelTypeId"") 
            p.*
        FROM ""Prices"" p
        INNER JOIN ""GasStations"" gs ON p.""GasStationId"" = gs.""Id""
        WHERE gs.""IsActive"" = true
          AND gs.""Latitude"" != 0 AND gs.""Longitude"" != 0
          AND ST_DWithin(gs.""Location"", ST_GeogFromText({0}), {1})
    ";

        if (fuelTypeId.HasValue)
        {
            sql += " AND p.\"FuelTypeId\" = {2}";
        }

        sql += " ORDER BY p.\"GasStationId\", p.\"FuelTypeId\", p.\"UpdatedAt\" DESC";

        var parameters = fuelTypeId.HasValue
            ? new object[] { pointWkt, radiusKm * 1000, fuelTypeId.Value }
            : new object[] { pointWkt, radiusKm * 1000 };

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

    public async Task UpdatePriceHistoryAsync(int gasStationId, int fuelTypeId, decimal amount, string source)
    {
        const string sql = @"
        MERGE INTO ""PriceHistory"" AS target
        USING (SELECT {0} AS ""GasStationId"", {1} AS ""FuelTypeId"", {2} AS ""Amount"", {3} AS ""Source"") AS source
        ON target.""GasStationId"" = source.""GasStationId""
           AND target.""FuelTypeId"" = source.""FuelTypeId""
           AND target.""ValidTo"" IS NULL
        WHEN MATCHED AND target.""Amount"" IS DISTINCT FROM source.""Amount"" THEN
            UPDATE SET ""ValidTo"" = NOW()
        WHEN NOT MATCHED THEN
            INSERT (""GasStationId"", ""FuelTypeId"", ""Amount"", ""Source"", ""ValidFrom"", ""ValidTo"")
            VALUES (source.""GasStationId"", source.""FuelTypeId"", source.""Amount"", source.""Source"", NOW(), NULL);";

        await _context.Database.ExecuteSqlRawAsync(sql, gasStationId, fuelTypeId, amount, source);
    }
    
    public async Task<IEnumerable<PriceHistoryDto>> GetPriceHistoryAsync(int stationId, int fuelTypeId, int days)
    {
        var cutoff = DateTime.UtcNow.AddDays(-days);
    
        var history = await _context.PriceHistory
            .Where(ph => ph.GasStationId == stationId
                         && ph.FuelTypeId == fuelTypeId
                         && ph.ValidFrom >= cutoff)
            .Select(ph => new { ph.ValidFrom, ph.Amount })
            .ToListAsync();

        var daily = history
            .GroupBy(x => x.ValidFrom.Date)
            .Select(g => new PriceHistoryDto
            {
                Date = g.Key,
                Price = g.OrderByDescending(x => x.ValidFrom).First().Amount
            })
            .OrderBy(d => d.Date)
            .ToList();

        return daily;
    }
    
    public async Task<IEnumerable<PriceHistoryDto>> GetBrandPriceHistoryAsync(string brand, int fuelTypeId, int days)
    {
        var cutoff = DateTime.UtcNow.AddDays(-days);
        brand = brand.Equals("F24", StringComparison.OrdinalIgnoreCase) ? "F24" : "Q8";

        var history = await _context.PriceHistory
            .Join(_context.GasStations,
                ph => ph.GasStationId,
                gs => gs.Id,
                (ph, gs) => new { ph, gs })
            .Where(x => x.gs.Brand == brand
                        && x.ph.FuelTypeId == fuelTypeId
                        && x.ph.ValidFrom >= cutoff)
            .Select(x => new { x.ph.ValidFrom, x.ph.Amount })
            .ToListAsync();

        var daily = history
            .GroupBy(x => x.ValidFrom.Date)
            .Select(g => new PriceHistoryDto
            {
                Date = g.Key,
                Price = g.OrderByDescending(x => x.ValidFrom).First().Amount
            })
            .OrderBy(d => d.Date)
            .ToList();

        return daily;
    }
}