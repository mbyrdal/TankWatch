using TankWatch.Core.DTOs;
using TankWatch.Core.Entities;
using TankWatch.Core.Interfaces;

namespace TankWatch.Infrastructure.Services;

public class GasStationService : IGasStationService
{
    private readonly IGasStationRepository _repository;

    public GasStationService(IGasStationRepository repository)
    {
        _repository = repository;
    }
    
    public async Task<IEnumerable<GasStationDto>> GetAllStationsAsync()
    {
        var stations = await _repository.GetAllAsync();
        return stations.Select(MapToDto);
    }
    
    public async Task<GasStationDto?> GetStationByIdAsync(int id)
    {
        var station = await _repository.GetByIdAsync(id);
        return station == null ? null : MapToDto(station);
    }
    
    public async Task<GasStationDto> CreateStationAsync(CreateGasStationDto dto)
    {
        var station = new GasStation
        {
            ExternalId = dto.ExternalId,
            Name = dto.Name,
            Brand = dto.Brand,
            Address = dto.Address,
            City = dto.City,
            PostalCode = dto.PostalCode,
            Latitude = dto.Latitude,
            Longitude = dto.Longitude,
            IsActive = dto.IsActive,
            LastUpdated = DateTime.UtcNow
        };

        var created = await _repository.AddAsync(station);
        return MapToDto(created);
    }
    
    public async Task UpdateStationAsync(int id, CreateGasStationDto dto)
    {
        var existing = await _repository.GetByIdAsync(id);
        if (existing == null)
            throw new KeyNotFoundException($"Station with id {id} not found.");

        existing.ExternalId = dto.ExternalId;
        existing.Name = dto.Name;
        existing.Brand = dto.Brand;
        existing.Address = dto.Address;
        existing.City = dto.City;
        existing.PostalCode = dto.PostalCode;
        existing.Latitude = dto.Latitude;
        existing.Longitude = dto.Longitude;
        existing.IsActive = dto.IsActive;
        // LastUpdated set in repository

        await _repository.UpdateAsync(existing);
    }
    
    public async Task DeleteStationAsync(int id)
    {
        await _repository.DeleteAsync(id);
    }
    
    private static GasStationDto MapToDto(GasStation station)
    {
        return new GasStationDto
        {
            Id = station.Id,
            ExternalId = station.ExternalId,
            Name = station.Name,
            Brand = station.Brand,
            Address = station.Address,
            City = station.City,
            PostalCode = station.PostalCode,
            Latitude = station.Latitude,
            Longitude = station.Longitude,
            IsActive = station.IsActive,
            LastUpdated = station.LastUpdated
        };
    }
}