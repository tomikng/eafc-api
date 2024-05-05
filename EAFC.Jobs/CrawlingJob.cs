using EAFC.Services;
using EAFC.Notifications;
using Quartz;

namespace EAFC.Jobs;

public class CrawlingJob(PlayerDataCrawler crawler, INotificationService notificationService)
    : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        
        try
        {
            var players = await crawler.FetchNewlyAddedPlayersAsync();
            await notificationService.SendAsync(players);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Job execution failed: {ex}");
        }
    }
}