using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Globalization;
using HtmlAgilityPack;
using Microsoft.Extensions.Configuration;
using EAFC.Core.Models;

namespace EAFC.Services
{
    public class PlayerDataCrawler(IConfiguration configuration)
    {
        private readonly string _dataUrl = configuration["CrawlerSettings:PlayerDataURL"]!;
        private readonly HtmlWeb _web = new();

        public async Task<List<Player>> FetchAllPlayersAsync()
        {
            var allPlayers = new List<Player>();
            await FetchPlayersRecursively(_dataUrl, allPlayers);
            return allPlayers;
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
                    Console.Error.WriteLine("Detected a loop in pagination, stopping recursion.");
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
