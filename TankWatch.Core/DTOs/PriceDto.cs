namespace TankWatch.Core.DTOs;

public class PriceDto
{
    public int GasStationId { get; set; }
    public string StationName { get; set; } = string.Empty;
    public string FuelType { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime UpdatedAt { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}