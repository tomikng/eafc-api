using Microsoft.AspNetCore.Mvc;
using EAFC.Services;

namespace EAFC.Api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class PlayersController : ControllerBase
    {
        private readonly PlayerDataCrawler _crawler;

        public PlayersController(PlayerDataCrawler crawler)
        {
            _crawler = crawler;
        }

        [HttpGet("crawl")]
        public async Task<IActionResult> GetNewPlayers()
        {
            var players = await _crawler.FetchNewPlayersFromHtmlAsync();
            return Ok(players);
        }
        
        [HttpGet("latest-players")]
        public async Task<IActionResult> GetPlayers()
        {
            throw new NotImplementedException();
        }
    }
}