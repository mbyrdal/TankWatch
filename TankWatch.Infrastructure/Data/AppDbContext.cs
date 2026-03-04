using Microsoft.EntityFrameworkCore;
using TankWatch.Core.Entities;

namespace TankWatch.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public DbSet<FuelType> FuelTypes { get; set; }
    public DbSet<GasStation> GasStations { get; set; }
    public DbSet<Price> Prices { get; set; }
    
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // FuelType configuration
        modelBuilder.Entity<FuelType>(entity =>
        {
            entity.HasIndex(e => e.Code).IsUnique();
        });
        
        // GasStation configuration (using PostGIS indexing)
        modelBuilder.Entity<GasStation>(entity =>
        {
            entity.HasIndex(e => e.ExternalId);
            
            entity.Property(e => e.Location)
                .HasComputedColumnSql(
                    "ST_SetSRID(ST_MakePoint(\"Longitude\", \"Latitude\"), 4326)",
                    stored: true)
                .HasColumnType("geography");   // Use geography for spherical calculations
            
            // spatial index on Location column
            entity.HasIndex(e => e.Location)
                .HasMethod("GIST");
        });
        
        // Price configuration
        modelBuilder.Entity<Price>(entity =>
        {
            entity.HasIndex(e => new { e.GasStationId, e.FuelTypeId, e.UpdatedAt })
                .IsDescending(false, false, true); // latest first

            entity.Property(e => e.Amount).HasPrecision(5, 2);
        });
        
        // Seed fuel types
        modelBuilder.Entity<FuelType>().HasData(
            new FuelType { Id = 1, Name = "Diesel", Code = "D" },
            new FuelType { Id = 2, Name = "Benzin 95", Code = "95" },
            new FuelType { Id = 3, Name = "Benzin 98", Code = "98" }
        );
    }
}