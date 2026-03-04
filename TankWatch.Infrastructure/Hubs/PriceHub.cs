using Microsoft.AspNetCore.SignalR;

namespace TankWatch.Infrastructure.Hubs;

public class PriceHub : Hub
{
    public async Task SubscribeToStation(int stationId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"station-{stationId}");
    }

    public async Task UnsubscribeFromStation(int stationId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"station-{stationId}");
    }
}