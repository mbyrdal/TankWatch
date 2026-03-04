using System.ComponentModel.DataAnnotations;

namespace TankWatch.Core.DTOs;

public class ReportPriceDto
{
    [Required]
    public int StationId { get; set; }

    [Required]
    public int FuelTypeId { get; set; }

    [Required]
    [Range(0.01, 99.99)]
    public decimal Amount { get; set; }
}