namespace TankWatch.Core.Entities;

public class PriceHistory : BaseEntity
{
    public int GasStationId { get; set; }
    public GasStation GasStation { get; set; } = null!;

    public int FuelTypeId { get; set; }
    public FuelType FuelType { get; set; } = null!;

    public decimal Amount { get; set; }
    public string Source { get; set; } = string.Empty; // "scraper", "user", etc.

    public DateTime ValidFrom { get; set; }   // start of validity
    public DateTime? ValidTo { get; set; }    // null = currently active
}