using EAFC.Notifications.interfaces;
using Microsoft.Extensions.Configuration;

namespace EAFC.Notifications;

using System.Reflection;

public class NotificationServiceFactory : INotificationServiceFactory
{
    public INotificationService CreateNotificationService(IServiceProvider serviceProvider, IConfiguration configuration)
    {
        if (!bool.TryParse(configuration["EnableNotifications"], out var enableNotifications) || !enableNotifications)
        {
            throw new InvalidOperationException("Notifications are disabled.");
        }

        var allowedPlatforms = configuration.GetSection("AllowedPlatforms").Get<string[]>();
        if (allowedPlatforms == null || allowedPlatforms.Length == 0)
        {
            throw new InvalidOperationException("No allowed platforms specified.");
        }

        foreach (var platform in allowedPlatforms)
        {
            var assemblyName = $"EAFC.{platform}Bot";
            var typeName = $"EAFC.{platform}Bot.{platform}NotificationService";
            var assembly = Assembly.Load(assemblyName);
            var type = assembly.GetType(typeName);
            if (type == null) continue;
            var instance = Activator.CreateInstance(type, serviceProvider, configuration);
            if (instance is INotificationService notificationService)
            {
                return notificationService;
            }
        }

        throw new InvalidOperationException("No valid notification service found.");
    }
}

