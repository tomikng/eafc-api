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
        [HttpGet("all-latest")]
        public async Task<IActionResult> GetPlayers(int page = 1, int pageSize = 100)
        {
            var result = await playerService.GetLatestPlayersAsync(page, pageSize);
            return Ok(result);
        }
        
        [HttpGet("latest")]
        public async Task<IActionResult> GetLatestPlayers(int page = 1, int pageSize = 100)
        {
            var result = await playerService.GetLatestPlayersByLatestAddOnAsync(page, pageSize);
            return Ok(result);
        }
    }
}