using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TankWatch.Infrastructure.Data;

namespace TankWatch.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FuelTypesController : ControllerBase
{
    private readonly AppDbContext _context;

    public FuelTypesController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<object>>> GetAll()
    {
        var fuelTypes = await _context.FuelTypes
            .Select(ft => new { ft.Id, ft.Name, ft.Code })
            .ToListAsync();
        return Ok(fuelTypes);
    }
}