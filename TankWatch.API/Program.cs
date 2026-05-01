using System.Net;
using Microsoft.EntityFrameworkCore;
using TankWatch.Core.Interfaces;
using TankWatch.Infrastructure.Data;
using TankWatch.Infrastructure.Geocoding;
using TankWatch.Infrastructure.Repositories;
using TankWatch.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Database context
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        o => o.UseNetTopologySuite()
    ));
    
// Repositories
builder.Services.AddScoped<IPriceRepository, PriceRepository>();
builder.Services.AddScoped<IGasStationRepository, GasStationRepository>();

// Services
builder.Services.AddScoped<IPriceService, PriceService>();
builder.Services.AddScoped<IGasStationService, GasStationService>();

// Nominatim geocoder
builder.Services.AddHttpClient<NominatimGeocoder>(client =>
{
    client.BaseAddress = new Uri("https://nominatim.openstreetmap.org/");
    
    // Nominatim requires a User-Agent header with your app name and contact email
    var email = builder.Configuration.GetValue<string>("NominatimPolicy:UserAgentEmail");
    
    if (string.IsNullOrEmpty(email))
        email = "mbyrdal10@gmail.com"; // fallback to avoid errors
    
    client.DefaultRequestHeaders.Add("User-Agent", $"TankWatch/1.0 ({email})");
});

// CircleK API client
builder.Services.AddHttpClient<CircleKFuelPriceProvider>(client =>
{
    client.BaseAddress = new Uri("https://api.circlek.com/eu/prices/");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
    client.DefaultRequestHeaders.Add("X-App-Name", "PRICES"); // Required header
})
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    { 
        // The Circle K API returns compressed responses (gzip/deflate).
        // Without automatic decompression, the response content would appear as binary
        // and cause JSON deserialization errors. This setting tells HttpClientHandler
        // to automatically decompress gzip and deflate encoded responses.
        AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
    });

// Q8/F24 API client
builder.Services.AddHttpClient<Q8FuelPriceProvider>(client =>
{
    client.BaseAddress = new Uri("https://beta.q8.dk/");
    client.Timeout = TimeSpan.FromSeconds(30); // reasonable timeout
    // No special headers required for this endpoint
});

// Background services
builder.Services.AddHostedService<PriceScraperService>();
builder.Services.AddHostedService<GeocodingBackgroundService>();

// Optional: Add Hangfire, Redis, etc.

// Add CORS policy (AFTER builder.Services.AddControllers() etc.)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
                "https://mbyrdal.github.io", // GH pages
                "http://localhost:5173" // local Vue dev server
            )
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseRouting();
app.UseCors("AllowFrontend");
app.UseAuthorization();
app.MapControllers();

// Health check
app.MapGet("/health", () => "OK");

app.Run();