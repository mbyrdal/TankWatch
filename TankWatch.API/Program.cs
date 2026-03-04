using Microsoft.EntityFrameworkCore;
using TankWatch.Core.Interfaces;
using TankWatch.Infrastructure.Data;
using TankWatch.Infrastructure.Hubs;
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
builder.Services.AddScoped<INotificationService, SignalRNotificationService>();

// SignalR
builder.Services.AddSignalR();

// Optional: Add Hangfire, Redis, etc.

var app = builder.Build();

// Configure pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.MapHub<PriceHub>("/priceHub");

// Optional: Hangfire dashboard
// app.UseHangfireDashboard();

app.Run();