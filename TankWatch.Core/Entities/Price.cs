namespace TankWatch.Core.Entities;

public class Price : BaseEntity
{
    public int GasStationId { get; set; }
    public GasStation GasStation { get; set; } = null!;

    public int FuelTypeId { get; set; }
    public FuelType FuelType { get; set; } = null!;

    public decimal Amount { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string Source { get; set; } = string.Empty; // e.g., "scraper", "user"
}