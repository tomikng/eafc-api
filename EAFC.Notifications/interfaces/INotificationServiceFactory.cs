using Microsoft.Extensions.Configuration;

namespace EAFC.Notifications.interfaces;

public interface INotificationServiceFactory
{
    INotificationService? CreateNotificationService(IServiceProvider serviceProvider, IConfiguration configuration);
}