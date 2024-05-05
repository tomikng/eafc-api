using System.Globalization;
using HtmlAgilityPack;
using Microsoft.Extensions.Configuration;
using EAFC.Core.Models;
using EAFC.Services.Interfaces;

namespace EAFC.Services
{
    public class PlayerDataCrawler(IConfiguration configuration, IPlayerService playerService)
    {
        private readonly HtmlWeb _web = new();
        private readonly string _dataUrl = configuration["CrawlerSettings:PlayerDataURL"]!;


        public async Task FetchAllPlayersAsync()
        {
            var allPlayers = new List<Player>();
            await FetchPlayersRecursively(_dataUrl, allPlayers);
            Console.WriteLine("Fetched all players. Adding to database.");
            await playerService.AddPlayersAsync(allPlayers);
        }
        
        private async Task FetchPlayersRecursively(string url, List<Player> allPlayers)
        {
            var doc = await _web.LoadFromWebAsync(url);
            var players = ExtractPlayersFromPage(doc);
            allPlayers.AddRange(players);

            // Check for next page
            var nextPageLink = doc.DocumentNode.SelectSingleNode("//a[@aria-label='Next']");
            if (nextPageLink != null && !string.IsNullOrWhiteSpace(nextPageLink.GetAttributeValue("href", null)))
            {
                var nextPageUrl = configuration["BaseUrl"] + nextPageLink.GetAttributeValue("href", string.Empty);
                if (nextPageUrl != url)
                {
                    await FetchPlayersRecursively(nextPageUrl, allPlayers);
                }
                else
                {
                    await Console.Error.WriteLineAsync("Detected a loop in pagination, stopping recursion.");
                }
            }
        }

        private List<Player> ExtractPlayersFromPage(HtmlDocument doc)
        {
            var players = new List<Player>();
            var table = doc.DocumentNode.SelectSingleNode("//table[@class='table table-new-players']");
            if (table == null)
            {
                Console.Error.WriteLine("No players table found on the page.");
                return players;
            }

            var rows = table.SelectNodes(".//tr[starts-with(@class, 'player_tr')]");
            if (rows == null)
            {
                Console.Error.WriteLine("No player rows found in the table.");
                return players;
            }

            foreach (var row in rows)
            {
                var profileLinkNode = row.SelectSingleNode(".//a[@class='player_name_players_table']");
                if (profileLinkNode == null)
                {
                    Console.Error.WriteLine("Player name link not found.");
                    continue; // Skip this player if name link is missing
                }

                var player = new Player
                {
                    Name = profileLinkNode.InnerText.Trim(),
                    Rating = Convert.ToInt32(row.SelectSingleNode(".//td[2]/span")?.InnerText.Trim()),
                    Position = row.SelectSingleNode(".//td[3]")?.InnerText.Trim() ?? string.Empty,
                    AddedOn = DateTime.ParseExact(row.SelectSingleNode(".//td[8]")?.InnerText.Trim() ?? string.Empty,
                        "yyyy-MM-dd", CultureInfo.InvariantCulture),
                    ProfileUrl = configuration["BaseUrl"] + profileLinkNode.GetAttributeValue("href", string.Empty)
                };

                players.Add(player);
            }

            return players;
        }
        
    }
}
