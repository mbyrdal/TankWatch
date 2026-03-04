using System.ComponentModel.DataAnnotations;

namespace TankWatch.Core.DTOs;

public class CreateGasStationDto
{
    public string? ExternalId { get; set; }

    [Required(ErrorMessage = "Name is required")]
    [StringLength(200, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string Brand { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string Address { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string City { get; set; } = string.Empty;

    [Required]
    [RegularExpression(@"^\d{4}$", ErrorMessage = "Postal code must be 4 digits")]
    public string PostalCode { get; set; } = string.Empty;

    [Range(-90, 90)]
    public double Latitude { get; set; }

    [Range(-180, 180)]
    public double Longitude { get; set; }

    public bool IsActive { get; set; } = true;
}