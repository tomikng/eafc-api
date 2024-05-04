using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using EAFC.Core.Models;
using HtmlAgilityPack;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace EAFC.Crawler
{
    public class PlayerDataCrawler
    {
        private readonly HttpClient _httpClient;
        private readonly string _dataUrl;

        public PlayerDataCrawler(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _dataUrl = configuration["CrawlerSettings:PlayerDataURL"]!;
        }

        public async Task<List<Player>> FetchNewPlayersAsync()
        {
            var response = await _httpClient.GetAsync(_dataUrl);
            response.EnsureSuccessStatusCode();
            var data = await response.Content.ReadAsStringAsync();
            var players = JsonConvert.DeserializeObject<List<Player>>(data);
            return players ?? new List<Player>();
        }

        public async Task<List<Player>> FetchNewPlayersFromHtmlAsync()
        {
            var response = await _httpClient.GetAsync(_dataUrl);
            response.EnsureSuccessStatusCode();
            var htmlContent = await response.Content.ReadAsStringAsync();

            var htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(htmlContent);

            var players = new List<Player>();
            var nodes = htmlDocument.DocumentNode.SelectNodes("//div[contains(@class, 'player-info')]");

            foreach (var node in nodes)
            {
                var player = new Player
                {
                    Name = node.SelectSingleNode(".//h4").InnerText.Trim(),
                    // Add more properties based on your HTML structure
                };
                players.Add(player);
            }

            return players;
        }
    }
}