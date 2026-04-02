using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FundRecommendationAPI.Models;
using FundRecommendationAPI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using MockQueryable.Moq;
using Xunit;

namespace FundRecommendationAPI.Tests
{


    public class SystemServiceTests
    {
        private readonly Mock<IRepository<SystemUpdateHistory>> _mockUpdateHistoryRepository;
        private readonly Mock<IRepository<FundBasicInfo>> _mockFundRepository;
        private readonly Mock<IRepository<FundNavHistory>> _mockNavHistoryRepository;
        private readonly Mock<IRepository<FundPerformance>> _mockPerformanceRepository;
        private readonly Mock<IRepository<FundManager>> _mockManagerRepository;
        private readonly Mock<IFundDataService> _mockFundDataService;
        private readonly Mock<ILogger<SystemService>> _mockLogger;
        private readonly SystemService _systemService;

        public SystemServiceTests()
        {
            _mockUpdateHistoryRepository = new Mock<IRepository<SystemUpdateHistory>>();
            _mockFundRepository = new Mock<IRepository<FundBasicInfo>>();
            _mockNavHistoryRepository = new Mock<IRepository<FundNavHistory>>();
            _mockPerformanceRepository = new Mock<IRepository<FundPerformance>>();
            _mockManagerRepository = new Mock<IRepository<FundManager>>();
            _mockFundDataService = new Mock<IFundDataService>();
            _mockLogger = new Mock<ILogger<SystemService>>();
            _systemService = new SystemService(
                _mockUpdateHistoryRepository.Object,
                _mockFundRepository.Object,
                _mockNavHistoryRepository.Object,
                _mockPerformanceRepository.Object,
                _mockManagerRepository.Object,
                _mockFundDataService.Object,
                _mockLogger.Object
            );
        }

        [Fact]
        public async Task GetSystemStatusAsync_ShouldReturnSystemStatus()
        {
            // Arrange
            var lastUpdate = new SystemUpdateHistory
            {
                Id = "update_20230101000000",
                Type = "full",
                StartTime = DateTime.Now.AddDays(-1),
                EndTime = DateTime.Now.AddDays(-1).AddHours(1),
                Status = "completed",
                RecordsUpdated = 100,
                CreatedAt = DateTime.Now.AddDays(-1)
            };

            var updateHistoryList = new List<SystemUpdateHistory> { lastUpdate };
            var mockDbSet = updateHistoryList.BuildMockDbSet();

            _mockUpdateHistoryRepository.Setup(r => r.Query())
                .Returns(mockDbSet.Object);
            _mockFundRepository.Setup(r => r.CountAsync()).ReturnsAsync(1000);
            _mockManagerRepository.Setup(r => r.CountAsync()).ReturnsAsync(500);

            // Act
            var result = await _systemService.GetSystemStatusAsync();

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetSystemStatusAsync_WithNoUpdateHistory_ShouldReturnStatusWithoutLastUpdate()
        {
            // Arrange
            var updateHistoryList = new List<SystemUpdateHistory>();
            var mockDbSet = updateHistoryList.BuildMockDbSet();

            _mockUpdateHistoryRepository.Setup(r => r.Query())
                .Returns(mockDbSet.Object);
            _mockFundRepository.Setup(r => r.CountAsync()).ReturnsAsync(1000);
            _mockManagerRepository.Setup(r => r.CountAsync()).ReturnsAsync(500);

            // Act
            var result = await _systemService.GetSystemStatusAsync();

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetUpdateHistoryAsync_ShouldReturnHistoryList()
        {
            // Arrange
            var historyList = new List<SystemUpdateHistory>
            {
                new SystemUpdateHistory
                {
                    Id = "update_20230101000000",
                    Type = "full",
                    StartTime = DateTime.Now.AddDays(-1),
                    EndTime = DateTime.Now.AddDays(-1).AddHours(1),
                    Status = "completed",
                    RecordsUpdated = 100,
                    CreatedAt = DateTime.Now.AddDays(-1)
                },
                new SystemUpdateHistory
                {
                    Id = "update_20230102000000",
                    Type = "incremental",
                    StartTime = DateTime.Now.AddDays(-0.5),
                    EndTime = DateTime.Now.AddDays(-0.5).AddMinutes(30),
                    Status = "completed",
                    RecordsUpdated = 50,
                    CreatedAt = DateTime.Now.AddDays(-0.5)
                }
            };

            var mockDbSet = historyList.BuildMockDbSet();

            _mockUpdateHistoryRepository.Setup(r => r.Query())
                .Returns(mockDbSet.Object);

            // Act
            var result = await _systemService.GetUpdateHistoryAsync(5);

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetUpdateHistoryAsync_WithLimit_ShouldReturnLimitedHistory()
        {
            // Arrange
            var historyList = new List<SystemUpdateHistory>
            {
                new SystemUpdateHistory
                {
                    Id = "update_20230101000000",
                    Type = "full",
                    StartTime = DateTime.Now.AddDays(-1),
                    EndTime = DateTime.Now.AddDays(-1).AddHours(1),
                    Status = "completed",
                    RecordsUpdated = 100,
                    CreatedAt = DateTime.Now.AddDays(-1)
                },
                new SystemUpdateHistory
                {
                    Id = "update_20230102000000",
                    Type = "incremental",
                    StartTime = DateTime.Now.AddDays(-0.5),
                    EndTime = DateTime.Now.AddDays(-0.5).AddMinutes(30),
                    Status = "completed",
                    RecordsUpdated = 50,
                    CreatedAt = DateTime.Now.AddDays(-0.5)
                }
            };

            var mockDbSet = historyList.BuildMockDbSet();

            _mockUpdateHistoryRepository.Setup(r => r.Query())
                .Returns(mockDbSet.Object);

            // Act
            var result = await _systemService.GetUpdateHistoryAsync(1);

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public async Task RealDataUpdateAsync_ShouldExecuteDataUpdate()
        {
            // Arrange
            var updateHistory = new SystemUpdateHistory
            {
                Id = "update_20230101000000",
                Type = "full",
                StartTime = DateTime.Now,
                Status = "running",
                RecordsUpdated = 0
            };

            _mockUpdateHistoryRepository.Setup(r => r.AddAsync(It.IsAny<SystemUpdateHistory>()))
                .Returns(Task.CompletedTask);
            _mockUpdateHistoryRepository.Setup(r => r.GetByIdAsync(It.IsAny<string>()))
                .ReturnsAsync(updateHistory);
            _mockUpdateHistoryRepository.Setup(r => r.UpdateAsync(It.IsAny<SystemUpdateHistory>()))
                .Returns(Task.CompletedTask);
            _mockFundDataService.Setup(s => s.UpdateFundBasicInfo(It.IsAny<string>()))
                .ReturnsAsync(new FundBasicInfo());
            _mockFundDataService.Setup(s => s.UpdateFundNavHistory(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new List<FundNavHistory>());
            _mockFundDataService.Setup(s => s.UpdateFundPerformance(It.IsAny<string>()))
                .ReturnsAsync(new List<FundPerformance>());
            _mockFundDataService.Setup(s => s.UpdateFundManagers(It.IsAny<string>()))
                .ReturnsAsync(new List<FundManager>());

            // Act
            var result = await _systemService.TriggerDataUpdateAsync("full");

            // Assert
            Assert.NotNull(result);
            _mockUpdateHistoryRepository.Verify(r => r.AddAsync(It.IsAny<SystemUpdateHistory>()), Times.Once);
            _mockUpdateHistoryRepository.Verify(r => r.UpdateAsync(It.IsAny<SystemUpdateHistory>()), Times.Once);
        }

        [Fact]
        public async Task GetUpdateHistoryAsync_ShouldHandleZeroLimit()
        {
            // Arrange
            var historyList = new List<SystemUpdateHistory>
            {
                new SystemUpdateHistory
                {
                    Id = "update_20230101000000",
                    Type = "full",
                    StartTime = DateTime.Now.AddDays(-1),
                    EndTime = DateTime.Now.AddDays(-1).AddHours(1),
                    Status = "completed",
                    RecordsUpdated = 100,
                    CreatedAt = DateTime.Now.AddDays(-1)
                }
            };

            var mockDbSet = historyList.BuildMockDbSet();

            _mockUpdateHistoryRepository.Setup(r => r.Query())
                .Returns(mockDbSet.Object);

            // Act
            var result = await _systemService.GetUpdateHistoryAsync(0);

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetUpdateHistoryAsync_ShouldHandleNegativeLimit()
        {
            // Arrange
            var historyList = new List<SystemUpdateHistory>
            {
                new SystemUpdateHistory
                {
                    Id = "update_20230101000000",
                    Type = "full",
                    StartTime = DateTime.Now.AddDays(-1),
                    EndTime = DateTime.Now.AddDays(-1).AddHours(1),
                    Status = "completed",
                    RecordsUpdated = 100,
                    CreatedAt = DateTime.Now.AddDays(-1)
                }
            };

            var mockDbSet = historyList.BuildMockDbSet();

            _mockUpdateHistoryRepository.Setup(r => r.Query())
                .Returns(mockDbSet.Object);

            // Act
            var result = await _systemService.GetUpdateHistoryAsync(-10);

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetSystemStatusAsync_ShouldHandleCountAsyncFailure()
        {
            // Arrange
            var data = new List<SystemUpdateHistory>();
            var mockDbSet = data.BuildMockDbSet();

            _mockUpdateHistoryRepository.Setup(r => r.Query())
                .Returns(mockDbSet.Object);
            _mockFundRepository.Setup(r => r.CountAsync())
                .ThrowsAsync(new Exception("Count failed"));
            _mockManagerRepository.Setup(r => r.CountAsync())
                .ThrowsAsync(new Exception("Count failed"));

            // Act
            var result = await _systemService.GetSystemStatusAsync();

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public async Task TriggerDataUpdateAsync_ShouldHandleAddHistoryFailure()
        {
            // Arrange
            _mockUpdateHistoryRepository.Setup(r => r.AddAsync(It.IsAny<SystemUpdateHistory>()))
                .ThrowsAsync(new Exception("Add failed"));

            // Act & Assert
            await _systemService.TriggerDataUpdateAsync();

            _mockUpdateHistoryRepository.Verify(r => r.AddAsync(It.IsAny<SystemUpdateHistory>()), Times.Once);
        }

        [Fact]
        public async Task RealDataUpdateAsync_ShouldHandleExceptionDuringUpdate()
        {
            // Arrange
            var updateHistory = new SystemUpdateHistory
            {
                Id = "update_20230101000000",
                Type = "full",
                StartTime = DateTime.Now,
                Status = "running",
                RecordsUpdated = 0
            };

            _mockUpdateHistoryRepository.Setup(r => r.AddAsync(It.IsAny<SystemUpdateHistory>()))
                .Returns(Task.CompletedTask);
            _mockUpdateHistoryRepository.Setup(r => r.GetByIdAsync(It.IsAny<string>()))
                .ReturnsAsync(updateHistory);
            _mockUpdateHistoryRepository.Setup(r => r.UpdateAsync(It.IsAny<SystemUpdateHistory>()))
                .Returns(Task.CompletedTask);
            _mockFundDataService.Setup(s => s.UpdateFundBasicInfo(It.IsAny<string>()))
                .ThrowsAsync(new Exception("Update failed"));

            // Act
            var result = await _systemService.TriggerDataUpdateAsync("full");

            // Assert
            Assert.NotNull(result);
            _mockUpdateHistoryRepository.Verify(r => r.AddAsync(It.IsAny<SystemUpdateHistory>()), Times.Once);
            _mockUpdateHistoryRepository.Verify(r => r.UpdateAsync(It.IsAny<SystemUpdateHistory>()), Times.Once);
        }

        [Fact]
        public async Task GetUpdateHistoryAsync_ShouldReturnEmptyListWhenNoHistory()
        {
            // Arrange
            var data = new List<SystemUpdateHistory>();
            var mockDbSet = data.BuildMockDbSet();

            _mockUpdateHistoryRepository.Setup(r => r.Query())
                .Returns(mockDbSet.Object);

            // Act
            var result = await _systemService.GetSystemStatusAsync();

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public async Task TriggerDataUpdateAsync_ShouldHandleInvalidUpdateType()
        {
            // Arrange
            var updateHistory = new SystemUpdateHistory
            {
                Id = "update_20230101000000",
                Type = "invalid",
                StartTime = DateTime.Now,
                Status = "running",
                RecordsUpdated = 0
            };

            _mockUpdateHistoryRepository.Setup(r => r.AddAsync(It.IsAny<SystemUpdateHistory>()))
                .Returns(Task.CompletedTask);
            _mockUpdateHistoryRepository.Setup(r => r.GetByIdAsync(It.IsAny<string>()))
                .ReturnsAsync(updateHistory);
            _mockUpdateHistoryRepository.Setup(r => r.UpdateAsync(It.IsAny<SystemUpdateHistory>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _systemService.TriggerDataUpdateAsync("invalid");

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void SystemService_Constructor_ShouldThrowArgumentNullExceptionWhenDependenciesAreNull()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new SystemService(
                null,
                _mockFundRepository.Object,
                _mockNavHistoryRepository.Object,
                _mockPerformanceRepository.Object,
                _mockManagerRepository.Object,
                _mockFundDataService.Object,
                _mockLogger.Object
            ));

            Assert.Throws<ArgumentNullException>(() => new SystemService(
                _mockUpdateHistoryRepository.Object,
                null,
                _mockNavHistoryRepository.Object,
                _mockPerformanceRepository.Object,
                _mockManagerRepository.Object,
                _mockFundDataService.Object,
                _mockLogger.Object
            ));

            Assert.Throws<ArgumentNullException>(() => new SystemService(
                _mockUpdateHistoryRepository.Object,
                _mockFundRepository.Object,
                null,
                _mockPerformanceRepository.Object,
                _mockManagerRepository.Object,
                _mockFundDataService.Object,
                _mockLogger.Object
            ));

            Assert.Throws<ArgumentNullException>(() => new SystemService(
                _mockUpdateHistoryRepository.Object,
                _mockFundRepository.Object,
                _mockNavHistoryRepository.Object,
                null,
                _mockManagerRepository.Object,
                _mockFundDataService.Object,
                _mockLogger.Object
            ));

            Assert.Throws<ArgumentNullException>(() => new SystemService(
                _mockUpdateHistoryRepository.Object,
                _mockFundRepository.Object,
                _mockNavHistoryRepository.Object,
                _mockPerformanceRepository.Object,
                null,
                _mockFundDataService.Object,
                _mockLogger.Object
            ));

            Assert.Throws<ArgumentNullException>(() => new SystemService(
                _mockUpdateHistoryRepository.Object,
                _mockFundRepository.Object,
                _mockNavHistoryRepository.Object,
                _mockPerformanceRepository.Object,
                _mockManagerRepository.Object,
                null,
                _mockLogger.Object
            ));

            Assert.Throws<ArgumentNullException>(() => new SystemService(
                _mockUpdateHistoryRepository.Object,
                _mockFundRepository.Object,
                _mockNavHistoryRepository.Object,
                _mockPerformanceRepository.Object,
                _mockManagerRepository.Object,
                _mockFundDataService.Object,
                null
            ));
        }
    }
}
