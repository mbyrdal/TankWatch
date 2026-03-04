using TankWatch.Core.DTOs;

namespace TankWatch.Core.Interfaces;

public interface IGasStationService
{
    Task<IEnumerable<GasStationDto>> GetAllStationsAsync();
    Task<GasStationDto?> GetStationByIdAsync(int id);
    Task<GasStationDto> CreateStationAsync(CreateGasStationDto dto);
    Task UpdateStationAsync(int id, CreateGasStationDto dto);
    Task DeleteStationAsync(int id);
}