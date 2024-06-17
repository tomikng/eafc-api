using EAFC.Core.Models;
using EAFC.Jobs;
using EAFC.Notifications;
using EAFC.Services.Interfaces;
using Moq;
using Quartz;

namespace EAFC.Tests
{
    [TestFixture]
    public class CrawlingJobTests
    {
        private Mock<IPlayerDataCrawler> _mockCrawler;
        private Mock<INotificationService> _mockNotificationService;
        private CrawlingJob _job;

        [SetUp]
        public void Setup()
        {
            _mockCrawler = new Mock<IPlayerDataCrawler>();
            _mockNotificationService = new Mock<INotificationService>();
            _job = new CrawlingJob(_mockCrawler.Object, _mockNotificationService.Object);
        }

        [Test]
        public async Task Execute_WithNewPlayers_SendsNotification()
        {
            var players = new List<Player>
            {
                new Player { Name = "Player 1", Rating = 1, Position = "Forward", AddedOn = new DateTime(2023, 1, 1), ProfileUrl = "url1" },
                new Player { Name = "Player 2", Rating = 2, Position = "Midfielder", AddedOn = new DateTime(2023, 1, 2), ProfileUrl = "url2" }
            };
            _mockCrawler.Setup(c => c.FetchNewlyAddedPlayersAsync()).ReturnsAsync(players);
            var mockContext = new Mock<IJobExecutionContext>();

            await _job.Execute(mockContext.Object);

            _mockNotificationService.Verify(n => n.SendNotificationAsync(players), Times.Once);
            _mockNotificationService.Verify(n => n.SendInfoNotificationAsync(It.IsAny<string>()), Times.Never);
            _mockNotificationService.Verify(n => n.SendErrorNotificationAsync(It.IsAny<string>()), Times.Never);
        }

        [Test]
        public async Task Execute_WithoutNewPlayers_SendsInfoNotification()
        {
            var players = new List<Player>();
            _mockCrawler.Setup(c => c.FetchNewlyAddedPlayersAsync()).ReturnsAsync(players);
            var mockContext = new Mock<IJobExecutionContext>();

            await _job.Execute(mockContext.Object);

            _mockNotificationService.Verify(n => n.SendNotificationAsync(It.IsAny<List<Player>>()), Times.Never);
            _mockNotificationService.Verify(n => n.SendInfoNotificationAsync("No new players found."), Times.Once);
            _mockNotificationService.Verify(n => n.SendErrorNotificationAsync(It.IsAny<string>()), Times.Never);
        }

        [Test]
        public async Task Execute_WithException_SendsErrorNotification()
        {
            var exception = new Exception("Test exception");
            _mockCrawler.Setup(c => c.FetchNewlyAddedPlayersAsync()).ThrowsAsync(exception);
            var mockContext = new Mock<IJobExecutionContext>();

            await _job.Execute(mockContext.Object);

            _mockNotificationService.Verify(n => n.SendErrorNotificationAsync(It.Is<string>(s => s.Contains($"Job execution failed: {exception}"))), Times.Once);
        }
        
        [Test]
        public async Task Execute_WithNullPlayers_SendsErrorNotification()
        {
            _mockCrawler.Setup(c => c.FetchNewlyAddedPlayersAsync())!.ReturnsAsync((List<Player>)null!);
            var mockContext = new Mock<IJobExecutionContext>();

            await _job.Execute(mockContext.Object);

            _mockNotificationService.Verify(n => n.SendNotificationAsync(It.IsAny<List<Player>>()), Times.Never);
            _mockNotificationService.Verify(n => n.SendInfoNotificationAsync(It.IsAny<string>()), Times.Never);
            _mockNotificationService.Verify(n => n.SendErrorNotificationAsync(It.Is<string>(s => s.Contains("Job execution failed"))), Times.Once);
        }
    }
}
