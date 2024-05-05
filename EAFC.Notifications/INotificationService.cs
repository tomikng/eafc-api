namespace EAFC.Notifications;

public interface INotificationService
{
    Task SendAsync(string message);
}