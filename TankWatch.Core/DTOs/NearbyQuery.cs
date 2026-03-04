namespace TankWatch.Core.DTOs;

public class NearbyQuery
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double RadiusKm { get; set; } = 10;
    public int? FuelTypeId { get; set; }
}