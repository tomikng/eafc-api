using Microsoft.AspNetCore.Mvc;
using EAFC.Services;
using EAFC.Services.Interfaces;

namespace EAFC.Api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class PlayersController(PlayerDataCrawler crawler, IPlayerService playerService) : ControllerBase
    {
        [HttpGet("crawl")]
        public async Task<IActionResult> GetNewPlayers()
        {
            await crawler.FetchAllPlayersAsync();
            return Ok();
        }
        
        [HttpGet("latest-players")]
        public async Task<IActionResult> GetPlayers()
        {
            var players = await playerService.GetLatestPlayersAsync();
            return Ok(players);
        }
    }
}