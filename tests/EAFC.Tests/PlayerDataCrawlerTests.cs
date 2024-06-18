using System.IO;
using System.Reflection;
using HtmlAgilityPack;
using Microsoft.Extensions.Configuration;
using Moq;
using EAFC.Core.Models;
using EAFC.Data;
using EAFC.Services;
using EAFC.Services.Interfaces;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;

namespace EAFC.Tests
{
    [TestFixture]
    public class PlayerDataCrawlerTests
    {
        private PlayerDataCrawler _playerDataCrawler;
        private Mock<IConfiguration> _mockConfiguration;
        private ApplicationDbContext _context;
        private IPlayerService _playerService;
        private HtmlDocument _customHtmlDocument;

        [SetUp]
        public void Setup()
        {
            // Initialize in-memory SQLite database
            var connection = new SqliteConnection("DataSource=:memory:");
            connection.Open();

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite(connection)
                .Options;

            _context = new ApplicationDbContext(options);
            _context.Database.EnsureCreated();

            _playerService = new PlayerService(_context);

            // Read the custom HTML file
            string htmlFilePath = "test_data.html";
            string htmlContent = File.ReadAllText(htmlFilePath);

            // Create an HtmlDocument object from the HTML content
            _customHtmlDocument = new HtmlDocument();
            _customHtmlDocument.LoadHtml(htmlContent);

            _mockConfiguration = new Mock<IConfiguration>();
            _mockConfiguration.Setup(c => c["BaseUrl"]).Returns("https://example.com");
            _mockConfiguration.Setup(c => c["CrawlerSettings:PlayerDataURL"]).Returns("https://example.com/players");

            _playerDataCrawler = new PlayerDataCrawler(_mockConfiguration.Object, _playerService);
        }

        [TearDown]
        public void TearDown()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        [Test]
        public void ExtractPlayersFromPage_ValidHtml_ReturnsPlayers()
        {
            // Arrange
            var extractPlayersFromPageMethod = typeof(PlayerDataCrawler).GetMethod("ExtractPlayersFromPage", BindingFlags.NonPublic | BindingFlags.Instance);

            // Act
            if (extractPlayersFromPageMethod == null) throw new MissingMethodException("Method not found");
            var players = (List<Player>)extractPlayersFromPageMethod.Invoke(_playerDataCrawler, new object[] { _customHtmlDocument })!;

            // Assert
            Assert.That(players, Is.Not.Empty);
            Assert.That(players.Count, Is.EqualTo(2));

            Assert.That(players[0].Name, Is.EqualTo("Arda Güler"));
            Assert.That(players[0].Rating, Is.EqualTo(95));
            Assert.That(players[0].Position, Is.EqualTo("CAM"));
            Assert.That(players[0].AddedOn, Is.EqualTo(new DateTime(2024, 6, 17)));
            Assert.That(players[0].ProfileUrl, Is.EqualTo("https://example.com/24/player/27121/arda-guler"));

            Assert.That(players[1].Name, Is.EqualTo("Adrien Rabiot"));
            Assert.That(players[1].Rating, Is.EqualTo(93));
            Assert.That(players[1].Position, Is.EqualTo("CM"));
            Assert.That(players[1].AddedOn, Is.EqualTo(new DateTime(2024, 6, 16)));
            Assert.That(players[1].ProfileUrl, Is.EqualTo("https://example.com/24/player/27120/adrien-rabiot"));
        }

        [Test]
        public void ExtractPlayersFromPage_InvalidHtmlStructure_HandlesGracefully()
        {
            // Arrange
            string invalidHtmlContent = "<html><body><div>Invalid Structure</div></body></html>";
            _customHtmlDocument.LoadHtml(invalidHtmlContent);
            var extractPlayersFromPageMethod = typeof(PlayerDataCrawler).GetMethod("ExtractPlayersFromPage", BindingFlags.NonPublic | BindingFlags.Instance);

            // Act
            if (extractPlayersFromPageMethod == null) throw new MissingMethodException("Method not found");
            var players = (List<Player>)extractPlayersFromPageMethod.Invoke(_playerDataCrawler, new object[] { _customHtmlDocument })!;

            // Assert
            Assert.That(players, Is.Empty);
        }

        [Test]
        public void ExtractPlayersFromPage_IncompletePlayerData_HandlesGracefully()
        {
            // Arrange
            string incompleteDataHtml = @"
            <html>
            <body>
                <table class='players-table'>
                    <tbody class='with-border with-background'>
                        <tr class='player-row'>
                            <td class='table-name'>
                                <div class='table-player-info'><a href='/24/player/27121/arda-guler' class='table-player-name'>Arda Güler</a></div>
                            </td>
                            <td class='table-rating'></td>
                            <td class='table-pos'></td>
                            <td class='table-cross-price'></td>
                            <td class='table-cross-range'></td>
                            <td class='table-pc-price'></td>
                            <td class='table-pc-range'></td>
                            <td class='table-added-on'></td>
                        </tr>
                    </tbody>
                </table>
            </body>
            </html>";

            _customHtmlDocument.LoadHtml(incompleteDataHtml);
            var extractPlayersFromPageMethod = typeof(PlayerDataCrawler).GetMethod("ExtractPlayersFromPage", BindingFlags.NonPublic | BindingFlags.Instance);

            // Act
            if (extractPlayersFromPageMethod == null) throw new MissingMethodException("Method not found");
            var players = (List<Player>)extractPlayersFromPageMethod.Invoke(_playerDataCrawler, new object[] { _customHtmlDocument })!;

            // Assert
            Assert.That(players, Is.Empty);
        }

        [Test]
        public void ExtractPlayersFromPage_InvalidRatingFormat_HandlesGracefully()
        {
            // Arrange
            string invalidRatingHtml = @"
            <html>
            <body>
                <table class='players-table'>
                    <tbody class='with-border with-background'>
                        <tr class='player-row'>
                            <td class='table-name'>
                                <div class='table-player-info'><a href='/24/player/27121/arda-guler' class='table-player-name'>Arda Güler</a></div>
                            </td>
                            <td class='table-rating'><div class='rating-square round-corner-small'>InvalidRating</div></td>
                            <td class='table-pos'>CAM</td>
                            <td class='table-cross-price'>178.9K</td>
                            <td class='table-cross-range'>0<i class='fa-solid fa-arrow-right-long range-arrow'></i>0</td>
                            <td class='table-pc-price'>162.6K</td>
                            <td class='table-pc-range'>0<i class='fa-solid fa-arrow-right-long range-arrow'></i>0</td>
                            <td class='table-added-on'>2024-06-17</td>
                        </tr>
                    </tbody>
                </table>
            </body>
            </html>";

            _customHtmlDocument.LoadHtml(invalidRatingHtml);
            var extractPlayersFromPageMethod = typeof(PlayerDataCrawler).GetMethod("ExtractPlayersFromPage", BindingFlags.NonPublic | BindingFlags.Instance);

            // Act
            if (extractPlayersFromPageMethod == null) throw new MissingMethodException("Method not found");
            var players = (List<Player>)extractPlayersFromPageMethod.Invoke(_playerDataCrawler, new object[] { _customHtmlDocument })!;

            // Assert
            Assert.That(players, Is.Empty);
        }
    }
}
