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
            await crawler.FetchAllPlayersAsync();
            await notificationService.SendAsync("Crawling completed and data updated.");
        }
        catch (Exception ex)
        {
            await notificationService.SendAsync($"Error during crawling: {ex.Message}");
            Console.WriteLine($"Job execution failed: {ex}");
        }
    }
}