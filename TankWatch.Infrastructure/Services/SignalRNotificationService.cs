using Microsoft.AspNetCore.SignalR;
using TankWatch.Core.Entities;
using TankWatch.Core.Interfaces;
using TankWatch.Infrastructure.Hubs;

namespace TankWatch.Infrastructure.Services;

public class SignalRNotificationService : INotificationService
{
    private readonly IHubContext<PriceHub> _hubContext;

    public SignalRNotificationService(IHubContext<PriceHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task NotifyPriceUpdate(int stationId, Price price)
    {
        await _hubContext.Clients.Group($"station-{stationId}").SendAsync("PriceUpdated", price);
    }
}