using Microsoft.AspNetCore.Mvc;
using TankWatch.Core.DTOs;
using TankWatch.Core.Interfaces;

namespace TankWatch.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GasStationsController : ControllerBase
{
    private readonly IGasStationService _gasStationService;

    public GasStationsController(IGasStationService gasStationService)
    {
        _gasStationService = gasStationService;
    }
    
    [HttpGet]
    public async Task<ActionResult<IEnumerable<GasStationDto>>> GetAll()
    {
        var stations = await _gasStationService.GetAllStationsAsync();
        return Ok(stations);
    }
    
    [HttpGet("{id}")]
    public async Task<ActionResult<GasStationDto>> GetById(int id)
    {
        var station = await _gasStationService.GetStationByIdAsync(id);
        if (station == null)
            return NotFound();
        return Ok(station);
    }
    
    [HttpPost]
    public async Task<ActionResult<GasStationDto>> Create(CreateGasStationDto dto)
    {
        var created = await _gasStationService.CreateStationAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }
    
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, CreateGasStationDto dto)
    {
        try
        {
            await _gasStationService.UpdateStationAsync(id, dto);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }
    
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _gasStationService.DeleteStationAsync(id);
        return NoContent();
    }
}