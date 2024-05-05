using EAFC.Core.Models;

namespace EAFC.Notifications;

public interface INotificationService
{
    Task SendAsync(List<Player> players);
}