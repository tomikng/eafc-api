using EAFC.Core.Models;
using EAFC.Data;
using EAFC.Services;
using EAFC.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EAFC.Tests
{
    [TestFixture]
    public class PlayerServiceTests
    {
        private ApplicationDbContext _context;
        private IPlayerService _playerService;

        [SetUp]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "PlayerDatabase")
                .Options;

            _context = new ApplicationDbContext(options);
            _playerService = new PlayerService(_context);
        }

        [TearDown]
        public void TearDown()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        [Test]
        public async Task GetLatestAddedOnDateAsync_ReturnsLatestDate()
        {
            // Arrange
            await _context.Players.AddRangeAsync(new List<Player>
            {
                new() { Name = "Player 1", AddedOn = new DateTime(2024, 6, 15), Position = "ST", ProfileUrl = "http://example.com/player1" },
                new() { Name = "Player 2", AddedOn = new DateTime(2024, 6, 17), Position = "CM", ProfileUrl = "http://example.com/player2" }
            });
            await _context.SaveChangesAsync();

            // Act
            var result = await _playerService.GetLatestAddedOnDateAsync();

            // Assert
            Assert.That(result, Is.EqualTo(new DateTime(2024, 6, 17)));
        }

        [Test]
        public async Task GetLatestPlayersAsync_ReturnsPagedPlayers()
        {
            // Arrange
            await _context.Players.AddRangeAsync(new List<Player>
            {
                new() { Name = "Player 1", AddedOn = new DateTime(2024, 6, 15), Position = "ST", ProfileUrl = "http://example.com/player1" },
                new() { Name = "Player 2", AddedOn = new DateTime(2024, 6, 16), Position = "CM", ProfileUrl = "http://example.com/player2" },
                new() { Name = "Player 3", AddedOn = new DateTime(2024, 6, 17), Position = "GK", ProfileUrl = "http://example.com/player3" }
            });
            await _context.SaveChangesAsync();

            // Act
            var result = await _playerService.GetLatestPlayersAsync(1, 2);

            Assert.Multiple(() =>
            {
                // Assert
                Assert.That(result.TotalCount, Is.EqualTo(3));
                Assert.That(result.Items, Has.Count.EqualTo(2));
            });
            Assert.Multiple(() =>
            {
                Assert.That(result.Items[0].Name, Is.EqualTo("Player 3"));
                Assert.That(result.Items[1].Name, Is.EqualTo("Player 2"));
            });
        }

        [Test]
        public async Task GetLatestPlayersByLatestAddOnAsync_ReturnsPlayersWithLatestAddOn()
        {
            // Arrange
            await _context.Players.AddRangeAsync(new List<Player>
            {
                new() { Name = "Player 1", AddedOn = new DateTime(2024, 6, 15), Position = "ST", ProfileUrl = "http://example.com/player1" },
                new() { Name = "Player 2", AddedOn = new DateTime(2024, 6, 17), Position = "CM", ProfileUrl = "http://example.com/player2" },
                new() { Name = "Player 3", AddedOn = new DateTime(2024, 6, 17), Position = "GK", ProfileUrl = "http://example.com/player3" }
            });
            await _context.SaveChangesAsync();

            // Act
            var result = await _playerService.GetLatestPlayersByLatestAddOnAsync(1, 2);

            Assert.Multiple(() =>
            {
                // Assert
                Assert.That(result.TotalCount, Is.EqualTo(2));
                Assert.That(result.Items.Count, Is.EqualTo(2));
            });
            Assert.Multiple(() =>
            {
                Assert.That(result.Items[0].Name, Is.EqualTo("Player 2"));
                Assert.That(result.Items[1].Name, Is.EqualTo("Player 3"));
            });
        }

        [Test]
        public async Task AddPlayersAsync_AddsNewPlayers()
        {
            // Arrange
            var playersToAdd = new List<Player>
            {
                new() { Name = "Player 1", AddedOn = new DateTime(2024, 6, 15), Position = "ST", ProfileUrl = "http://example.com/player1" },
                new() { Name = "Player 2", AddedOn = new DateTime(2024, 6, 17), Position = "CM", ProfileUrl = "http://example.com/player2" }
            };

            // Act
            await _playerService.AddPlayersAsync(playersToAdd);

            // Assert
            var players = await _context.Players.ToListAsync();
            Assert.That(players.Count, Is.EqualTo(2));
            Assert.Multiple(() =>
            {
                Assert.That(players.Any(p => p.Name == "Player 1" && p.AddedOn == new DateTime(2024, 6, 15)), Is.True);
                Assert.That(players.Any(p => p.Name == "Player 2" && p.AddedOn == new DateTime(2024, 6, 17)), Is.True);
            });
        }

        [Test]
        public async Task AddPlayersAsync_DoesNotAddDuplicatePlayers()
        {
            // Arrange
            await _context.Players.AddAsync(new Player { Name = "Player 1", AddedOn = new DateTime(2024, 6, 15), Position = "ST", ProfileUrl = "http://example.com/player1" });
            await _context.SaveChangesAsync();

            var playersToAdd = new List<Player>
            {
                new() { Name = "Player 1", AddedOn = new DateTime(2024, 6, 15), Position = "ST", ProfileUrl = "http://example.com/player1" },
                new() { Name = "Player 2", AddedOn = new DateTime(2024, 6, 17), Position = "CM", ProfileUrl = "http://example.com/player2" }
            };

            // Act
            await _playerService.AddPlayersAsync(playersToAdd);

            // Assert
            var players = await _context.Players.ToListAsync();
            Assert.That(players.Count, Is.EqualTo(2)); // Only one new player should be added
            Assert.That(players.Any(p => p.Name == "Player 2" && p.AddedOn == new DateTime(2024, 6, 17)), Is.True);
        }
        
        [Test]
        public async Task GetLatestAddedOnDateAsync_ReturnsNull_WhenDatabaseIsEmpty()
        {
            // Act
            var result = await _playerService.GetLatestAddedOnDateAsync();

            // Assert
            Assert.That(result, Is.Null);
        }

        [Test]
        public async Task GetLatestPlayersAsync_ReturnsEmptyList_WhenDatabaseIsEmpty()
        {
            // Act
            var result = await _playerService.GetLatestPlayersAsync();

            // Assert
            Assert.That(result.Items, Is.Empty);
            Assert.That(result.TotalCount, Is.EqualTo(0));
        }

        [Test]
        public async Task AddPlayersAsync_AddsSinglePlayer()
        {
            // Arrange
            var playerToAdd = new Player
            {
                Name = "Player 1",
                AddedOn = new DateTime(2024, 6, 15),
                Position = "ST",
                ProfileUrl = "http://example.com/player1"
            };

            // Act
            await _playerService.AddPlayersAsync(new[] { playerToAdd });

            // Assert
            var players = await _context.Players.ToListAsync();
            Assert.That(players.Count, Is.EqualTo(1));
            Assert.That(players[0].Name, Is.EqualTo("Player 1"));
        }

        [Test]
        public async Task GetLatestPlayersAsync_HandlesPaginationCorrectly()
        {
            // Arrange
            await _context.Players.AddRangeAsync(new List<Player>
            {
                new() { Name = "Player 1", AddedOn = new DateTime(2024, 6, 15), Position = "ST", ProfileUrl = "http://example.com/player1" },
                new() { Name = "Player 2", AddedOn = new DateTime(2024, 6, 16), Position = "CM", ProfileUrl = "http://example.com/player2" },
                new() { Name = "Player 3", AddedOn = new DateTime(2024, 6, 17), Position = "GK", ProfileUrl = "http://example.com/player3" },
                new() { Name = "Player 4", AddedOn = new DateTime(2024, 6, 18), Position = "LB", ProfileUrl = "http://example.com/player4" }
            });
            await _context.SaveChangesAsync();

            // Act
            var result = await _playerService.GetLatestPlayersAsync(2, 2);

            // Assert
            Assert.That(result.TotalCount, Is.EqualTo(4));
            Assert.That(result.Items.Count, Is.EqualTo(2));
            Assert.That(result.Items[0].Name, Is.EqualTo("Player 2"));
            Assert.That(result.Items[1].Name, Is.EqualTo("Player 1"));
        }

        [Test]
        public async Task AddPlayersAsync_HandlesDuplicateEntries()
        {
            // Arrange
            await _context.Players.AddAsync(new Player
            {
                Name = "Player 1",
                AddedOn = new DateTime(2024, 6, 15),
                Position = "ST",
                ProfileUrl = "http://example.com/player1"
            });
            await _context.SaveChangesAsync();

            var playersToAdd = new List<Player>
            {
                new Player { Name = "Player 1", AddedOn = new DateTime(2024, 6, 15), Position = "ST", ProfileUrl = "http://example.com/player1" },
                new Player { Name = "Player 2", AddedOn = new DateTime(2024, 6, 17), Position = "CM", ProfileUrl = "http://example.com/player2" }
            };

            // Act
            await _playerService.AddPlayersAsync(playersToAdd);

            // Assert
            var players = await _context.Players.ToListAsync();
            Assert.That(players.Count, Is.EqualTo(2)); // Only one new player should be added
            Assert.That(players.Any(p => p.Name == "Player 2" && p.AddedOn == new DateTime(2024, 6, 17)), Is.True);
        }

        [Test]
        public async Task GetLatestPlayersByLatestAddOnAsync_HandlesBoundaryDates()
        {
            // Arrange
            await _context.Players.AddRangeAsync(new List<Player>
            {
                new() { Name = "Player 1", AddedOn = DateTime.MinValue, Position = "ST", ProfileUrl = "http://example.com/player1" },
                new() { Name = "Player 2", AddedOn = DateTime.MaxValue, Position = "CM", ProfileUrl = "http://example.com/player2" }
            });
            await _context.SaveChangesAsync();

            // Act
            var result = await _playerService.GetLatestPlayersByLatestAddOnAsync(1, 2);

            Assert.Multiple(() =>
            {
                // Assert
                Assert.That(result.TotalCount, Is.EqualTo(1));
                Assert.That(result.Items.Count, Is.EqualTo(1));
            });
            Assert.That(result.Items[0].Name, Is.EqualTo("Player 2"));
        }
    }
}
