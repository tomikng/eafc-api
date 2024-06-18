using EAFC.Core.Models;

namespace EAFC.Notifications.interfaces;

public interface INotificationService
{
    Task SendNotificationAsync(List<Player> players);
    Task SendNotificationAsync(Player player);
    Task SendErrorNotificationAsync(string errorMessage);
    Task SendWarningNotificationAsync(string warningMessage);
    Task SendInfoNotificationAsync(string infoMessage);
    Task SendCustomNotificationAsync(string customMessage);
    Task SendNotificationAsync(string message);
}