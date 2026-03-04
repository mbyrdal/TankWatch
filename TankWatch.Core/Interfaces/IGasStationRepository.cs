using TankWatch.Core.Entities;

namespace TankWatch.Core.Interfaces;

public interface IGasStationRepository
{
    Task<IEnumerable<GasStation>> GetAllAsync();
    Task<GasStation?> GetByIdAsync(int id);
    Task<GasStation> AddAsync(GasStation station);
    Task UpdateAsync(GasStation station);
    Task DeleteAsync(int id); // soft delete: set IsActive = false
    Task<bool> ExistsAsync(int id);
}