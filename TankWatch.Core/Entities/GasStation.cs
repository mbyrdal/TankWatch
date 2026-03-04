using NetTopologySuite.Geometries;

namespace TankWatch.Core.Entities;

public class GasStation : BaseEntity
{
    public string? ExternalId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime LastUpdated { get; set; }

    // Computed column for PostGIS
    public Point? Location { get; set; }
    
    // Navigation
    public ICollection<Price>? Prices { get; set; }
}