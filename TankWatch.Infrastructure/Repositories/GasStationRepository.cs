using Microsoft.EntityFrameworkCore;
using TankWatch.Core.Entities;
using TankWatch.Core.Interfaces;
using TankWatch.Infrastructure.Data;

namespace TankWatch.Infrastructure.Repositories;

public class GasStationRepository : IGasStationRepository
{
    private readonly AppDbContext _context;

    public GasStationRepository(AppDbContext context)
    {
        _context = context;
    }
    
    public async Task<IEnumerable<GasStation>> GetAllAsync()
    {
        return await _context.GasStations
            .Where(gs => gs.IsActive) // optionally only active
            .ToListAsync();
    }
    
    public async Task<GasStation?> GetByIdAsync(int id)
    {
        return await _context.GasStations
            .FirstOrDefaultAsync(gs => gs.Id == id);
    }
    
    public async Task<GasStation> AddAsync(GasStation station)
    {
        station.LastUpdated = DateTime.UtcNow;
        _context.GasStations.Add(station);
        await _context.SaveChangesAsync();
        return station;
    }
    
    public async Task UpdateAsync(GasStation station)
    {
        station.LastUpdated = DateTime.UtcNow;
        _context.Entry(station).State = EntityState.Modified;
        await _context.SaveChangesAsync();
    }
    
    public async Task DeleteAsync(int id)
    {
        var station = await _context.GasStations.FindAsync(id);
        if (station != null)
        {
            station.IsActive = false; // soft delete
            station.LastUpdated = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }
    
    public async Task<bool> ExistsAsync(int id)
    {
        return await _context.GasStations.AnyAsync(gs => gs.Id == id);
    }
}