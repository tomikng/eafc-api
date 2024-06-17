using EAFC.Notifications;
using EAFC.Services.Interfaces;
using Quartz;

namespace EAFC.Jobs;

public class CrawlingJob(IPlayerDataCrawler crawler, INotificationService notificationService)
    : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        try
        {
            var players = await crawler.FetchNewlyAddedPlayersAsync();

            if (players.Count > 0)
            {
                await notificationService.SendNotificationAsync(players);
            }
            else
            {
                await notificationService.SendInfoNotificationAsync("No new players found.");
            }
        }
        catch (Exception ex)
        {
            await notificationService.SendErrorNotificationAsync($"Job execution failed: {ex}");
        }
    }
}