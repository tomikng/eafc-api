using System.Globalization;
using HtmlAgilityPack;
using Microsoft.Extensions.Configuration;
using EAFC.Core.Models;
using EAFC.Services.Interfaces;

namespace EAFC.Services
{
    public class PlayerDataCrawler(IConfiguration configuration, IPlayerService playerService) : IPlayerDataCrawler
    {
        private readonly HtmlWeb _web = new();
        private readonly string _dataUrl = configuration["CrawlerSettings:PlayerDataURL"] ?? throw new InvalidDataException();

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
            var tableBody = doc.DocumentNode.SelectSingleNode("//tbody[@class='with-border with-background']");
            if (tableBody == null)
            {
                Console.Error.WriteLine("No players table body found on the page.");
                return players;
            }

            var rows = tableBody.SelectNodes(".//tr[@class='player-row']");
            if (rows == null)
            {
                Console.Error.WriteLine("No player rows found in the table.");
                return players;
            }

            foreach (var row in rows)
            {
                var nameNode = row.SelectSingleNode(".//a[@class='table-player-name']");
                if (nameNode == null)
                {
                    Console.Error.WriteLine("Player name not found.");
                    continue; // Skip this player if name is missing
                }

                var player = new Player
                {
                    Name = nameNode.InnerText.Trim(),
                    Rating = Convert.ToInt32(row.SelectSingleNode(".//div[@class='rating-square round-corner-small']")?.InnerText.Trim()),
                    Position = row.SelectSingleNode(".//td[@class='table-pos']")?.InnerText.Trim() ?? string.Empty,
                    AddedOn = DateTime.ParseExact(row.SelectSingleNode(".//td[@class='table-added-on']")?.InnerText.Trim() ?? string.Empty,
                        "yyyy-MM-dd", CultureInfo.InvariantCulture),
                    ProfileUrl = configuration["BaseUrl"] + nameNode.GetAttributeValue("href", string.Empty)
                };

                players.Add(player);
            }

            return players;
        }

        public async Task<List<Player>> FetchNewlyAddedPlayersAsync()
        {
            // Fetch all players from the external source
            var allPlayers = new List<Player>();
            await FetchPlayersRecursively(_dataUrl, allPlayers);

            if (allPlayers.Count == 0)
            {
                Console.WriteLine("No players were fetched.");
                return [];
            }

            var latestDateInDb = await playerService.GetLatestAddedOnDateAsync() ?? DateTime.MinValue;

            var newPlayers = allPlayers.Where(p => p.AddedOn > latestDateInDb).ToList();

            if (newPlayers.Count != 0)
            {
                await playerService.AddPlayersAsync(newPlayers);
                return newPlayers;
            }
            else
            {
                return [];
            }
        }
    }
}
