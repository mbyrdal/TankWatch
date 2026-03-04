namespace TankWatch.Core.DTOs;

public class ReportPriceDto
{
    public int StationId { get; set; }
    public int FuelTypeId { get; set; }
    public decimal Amount { get; set; }
}