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
            HtmlDocument doc;
            try
            {
                doc = await _web.LoadFromWebAsync(url);
            }
            catch (Exception ex)
            {
                await Console.Error.WriteLineAsync($"Error fetching URL {url}: {ex.Message}");
                throw new Exception("Error fetching player data", ex);
            }
            
            var players = ExtractPlayersFromPage(doc);
            allPlayers.AddRange(players);

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

                int rating;
                try
                {
                    rating = Convert.ToInt32(row.SelectSingleNode(".//div[@class='rating-square round-corner-small']")?.InnerText.Trim());
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Error parsing rating for player {nameNode.InnerText.Trim()}: {ex.Message}");
                    continue;
                }

                DateTime addedOn;
                try
                {
                    addedOn = DateTime.ParseExact(row.SelectSingleNode(".//td[@class='table-added-on']")?.InnerText.Trim() ?? string.Empty,
                        "yyyy-MM-dd", CultureInfo.InvariantCulture);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Error parsing added date for player {nameNode.InnerText.Trim()}: {ex.Message}");
                    continue;
                }

                var player = new Player
                {
                    Name = nameNode.InnerText.Trim(),
                    Rating = rating,
                    Position = row.SelectSingleNode(".//td[@class='table-pos']")?.InnerText.Trim() ?? string.Empty,
                    AddedOn = addedOn,
                    ProfileUrl = configuration["BaseUrl"] + nameNode.GetAttributeValue("href", string.Empty)
                };

                players.Add(player);
            }

            return players;
        }

        public async Task<List<Player>> FetchNewlyAddedPlayersAsync()
        {
            if (string.IsNullOrWhiteSpace(_dataUrl))
            {
                await Console.Error.WriteLineAsync("Data URL is not configured.");
                throw new InvalidDataException("Data URL is not configured.");
            }

            var allPlayers = new List<Player>();
            await FetchPlayersRecursively(_dataUrl, allPlayers);

            if (allPlayers.Count == 0)
            {
                Console.WriteLine("No players were fetched.");
                return new List<Player>();
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
