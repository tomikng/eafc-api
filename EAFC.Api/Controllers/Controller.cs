using EAFC.Notifications;
using Microsoft.AspNetCore.Mvc;
using EAFC.Services;
using EAFC.Services.Interfaces;

namespace EAFC.Api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class PlayersController(PlayerDataCrawler crawler, IPlayerService playerService, INotificationService notificationService) : ControllerBase
    {
        [HttpGet("crawl")]
        public async Task<IActionResult> GetNewPlayers()
        {
            await crawler.FetchAllPlayersAsync();
            return Ok();
        }
        
        [HttpGet("all-latest")]
        public async Task<IActionResult> GetPlayers(int page = 1, int pageSize = 100)
        {
            var result = await playerService.GetLatestPlayersAsync(page, pageSize);
            return Ok(result);
        }
        
        [HttpGet("send-test-message")]
        public async Task<IActionResult> SendTestMessage()
        {
            string message = "Hello, this is a test message from the Discord bot!";
            await notificationService.SendAsync(message);
            return Ok("Message sent successfully.");
        }
        
        [HttpGet("latest")]
        public async Task<IActionResult> GetLatestPlayers(int page = 1, int pageSize = 100)
        {
            var result = await playerService.GetLatestPlayersByLatestAddOnAsync(page, pageSize);
            return Ok(result);
        }
    }
}