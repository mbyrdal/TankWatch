using Microsoft.AspNetCore.Mvc;
using TankWatch.Core.DTOs;
using TankWatch.Core.Interfaces;

namespace TankWatch.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PricesController : ControllerBase
{
    private readonly IPriceService _priceService;

    public PricesController(IPriceService priceService)
    {
        _priceService = priceService;
    }
    
    [HttpGet("nearby")]
    public async Task<ActionResult<IEnumerable<PriceDto>>> GetNearby([FromQuery] NearbyQuery query)
    {
        var result = await _priceService.GetNearbyPricesAsync(query);
        return Ok(result);
    }
    
    [HttpPost("report")]
    public async Task<IActionResult> Report([FromBody] ReportPriceDto report)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            await _priceService.ReportPriceAsync(report.StationId, report.FuelTypeId, report.Amount, "user");
            return Ok();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            // Log the exception
            return StatusCode(500, "An unexpected error occurred: "  + ex.Message);
        }
    }
    
    [HttpGet("history")]
    public async Task<ActionResult<IEnumerable<PriceHistoryDto>>> GetPriceHistory(
        int stationId, int fuelTypeId, int days = 30)
    {
        if (days <= 0 || days > 365) days = 30;
        var history = await _priceService.GetPriceHistoryAsync(stationId, fuelTypeId, days);
        return Ok(history);
    }
    
    [HttpGet("brand-history")]
    public async Task<ActionResult<IEnumerable<PriceHistoryDto>>> GetBrandPriceHistory(
        string brand, int fuelTypeId, int days = 30)
    {
        if (days <= 0 || days > 365) days = 30;
        var history = await _priceService.GetBrandPriceHistoryAsync(brand, fuelTypeId, days);
        return Ok(history);
    }
}