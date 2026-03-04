using TankWatch.Core.Entities;

namespace TankWatch.Core.Interfaces;

public interface INotificationService
{
    Task NotifyPriceUpdate(int stationId, Price price);
}