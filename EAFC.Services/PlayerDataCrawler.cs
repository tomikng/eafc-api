using EAFC.Core.Models;
using HtmlAgilityPack;
using Microsoft.Extensions.Configuration;

namespace EAFC.Services
{
    public class PlayerDataCrawler
    {
        private readonly HttpClient _httpClient;
        private readonly string _dataUrl;
        private readonly HtmlWeb _web;

        public PlayerDataCrawler(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _dataUrl = configuration["CrawlerSettings:PlayerDataURL"]!;
            _web = new HtmlWeb();
        }
        
        public async Task<List<Player>> FetchNewPlayersFromHtmlAsync()
        {
            var doc = await _web.LoadFromWebAsync(_dataUrl);
            var players = new List<Player>();

            var table = doc.DocumentNode.SelectSingleNode("//table[@class='table table-new-players']");
            var rows = table.SelectNodes(".//tr[starts-with(@class, 'player_tr')]");

            foreach (var row in rows)
            {
                var player = new Player
                {
                    Name = row.SelectSingleNode(".//a[@class='player_name_players_table']").InnerText.Trim(),
                    Rating = Convert.ToInt32(row.SelectSingleNode(".//td[2]/span").InnerText.Trim()),
                    Position = row.SelectSingleNode(".//td[3]").InnerText.Trim(),
                    AddedOn = Convert.ToDateTime(row.SelectSingleNode(".//td[8]").InnerText.Trim())
                };

                players.Add(player);
            }

            return players;
        }
    }
}