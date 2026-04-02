using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FundRecommendationAPI.Models;
using FundRecommendationAPI.Services;
using Moq;
using Xunit;

namespace FundRecommendationAPI.Tests
{
    public class FundDataServiceTests
    {
        private readonly Mock<IRepository<FundBasicInfo>> _mockFundRepository;
        private readonly Mock<IRepository<FundNavHistory>> _mockNavHistoryRepository;
        private readonly Mock<IRepository<FundPerformance>> _mockPerformanceRepository;
        private readonly Mock<IRepository<FundManager>> _mockManagerRepository;
        private readonly Mock<IRepository<FundAssetScale>> _mockAssetScaleRepository;
        private readonly Mock<IRepository<FundPurchaseStatus>> _mockPurchaseStatusRepository;
        private readonly Mock<IRepository<FundRedemptionStatus>> _mockRedemptionStatusRepository;
        private readonly Mock<IRepository<FundCorporateActions>> _mockCorporateActionsRepository;
        private readonly FundDataService _fundDataService;

        public FundDataServiceTests()
        {
            _mockFundRepository = new Mock<IRepository<FundBasicInfo>>();
            _mockNavHistoryRepository = new Mock<IRepository<FundNavHistory>>();
            _mockPerformanceRepository = new Mock<IRepository<FundPerformance>>();
            _mockManagerRepository = new Mock<IRepository<FundManager>>();
            _mockAssetScaleRepository = new Mock<IRepository<FundAssetScale>>();
            _mockPurchaseStatusRepository = new Mock<IRepository<FundPurchaseStatus>>();
            _mockRedemptionStatusRepository = new Mock<IRepository<FundRedemptionStatus>>();
            _mockCorporateActionsRepository = new Mock<IRepository<FundCorporateActions>>();

            _fundDataService = new FundDataService(
                _mockFundRepository.Object,
                _mockNavHistoryRepository.Object,
                _mockPerformanceRepository.Object,
                _mockManagerRepository.Object,
                _mockAssetScaleRepository.Object,
                _mockPurchaseStatusRepository.Object,
                _mockRedemptionStatusRepository.Object,
                _mockCorporateActionsRepository.Object
            );
        }

        [Fact]
        public async Task UpdateFundBasicInfo_ShouldReturnFundBasicInfo()
        {
            // Arrange
            var fundCode = "123456";
            var expectedFund = new FundBasicInfo
            {
                Code = fundCode,
                Name = "Test Fund",
                FundType = "股票型",
                EstablishDate = DateOnly.FromDateTime(DateTime.Now.AddYears(-3)),
                Manager = "Test Manager",
                ManagementFeeRate = 0.015m,
                CustodianFeeRate = 0.0025m,
                UpdateTime = DateTime.Now
            };

            _mockFundRepository.Setup(r => r.AddAsync(It.IsAny<FundBasicInfo>()))
                .Returns(Task.CompletedTask);
            _mockFundRepository.Setup(r => r.UpdateAsync(It.IsAny<FundBasicInfo>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _fundDataService.UpdateFundBasicInfo(fundCode);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(fundCode, result.Code);
        }

        [Fact]
        public async Task UpdateFundNavHistory_ShouldReturnNavHistoryList()
        {
            // Arrange
            var fundCode = "123456";
            var startDate = "2023-01-01";
            var endDate = "2023-01-31";

            _mockNavHistoryRepository.Setup(r => r.AddAsync(It.IsAny<FundNavHistory>()))
                .Returns(Task.CompletedTask);
            _mockNavHistoryRepository.Setup(r => r.UpdateAsync(It.IsAny<FundNavHistory>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _fundDataService.UpdateFundNavHistory(fundCode, startDate, endDate);

            // Assert
            Assert.NotNull(result);
            Assert.IsType<List<FundNavHistory>>(result);
        }

        [Fact]
        public async Task UpdateFundPerformance_ShouldReturnPerformanceList()
        {
            // Arrange
            var fundCode = "123456";

            _mockPerformanceRepository.Setup(r => r.AddAsync(It.IsAny<FundPerformance>()))
                .Returns(Task.CompletedTask);
            _mockPerformanceRepository.Setup(r => r.UpdateAsync(It.IsAny<FundPerformance>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _fundDataService.UpdateFundPerformance(fundCode);

            // Assert
            Assert.NotNull(result);
            Assert.IsType<List<FundPerformance>>(result);
        }

        [Fact]
        public async Task UpdateFundManagers_ShouldReturnManagerList()
        {
            // Arrange
            var fundCode = "123456";

            _mockManagerRepository.Setup(r => r.AddAsync(It.IsAny<FundManager>()))
                .Returns(Task.CompletedTask);
            _mockManagerRepository.Setup(r => r.UpdateAsync(It.IsAny<FundManager>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _fundDataService.UpdateFundManagers(fundCode);

            // Assert
            Assert.NotNull(result);
            Assert.IsType<List<FundManager>>(result);
        }

        [Fact]
        public async Task UpdateFundBasicInfo_ShouldHandleException()
        {
            // Arrange
            var fundCode = "123456";

            _mockFundRepository.Setup(r => r.AddAsync(It.IsAny<FundBasicInfo>()))
                .ThrowsAsync(new Exception("Add failed"));

            // Act & Assert
            var result = await _fundDataService.UpdateFundBasicInfo(fundCode);
            Assert.NotNull(result);
        }

        [Fact]
        public async Task UpdateFundNavHistory_ShouldHandleException()
        {
            // Arrange
            var fundCode = "123456";
            var startDate = "2023-01-01";
            var endDate = "2023-01-31";

            _mockNavHistoryRepository.Setup(r => r.AddAsync(It.IsAny<FundNavHistory>()))
                .ThrowsAsync(new Exception("Add failed"));

            // Act & Assert
            var result = await _fundDataService.UpdateFundNavHistory(fundCode, startDate, endDate);
            Assert.NotNull(result);
        }

        [Fact]
        public async Task UpdateFundPerformance_ShouldHandleException()
        {
            // Arrange
            var fundCode = "123456";

            _mockPerformanceRepository.Setup(r => r.AddAsync(It.IsAny<FundPerformance>()))
                .ThrowsAsync(new Exception("Add failed"));

            // Act & Assert
            var result = await _fundDataService.UpdateFundPerformance(fundCode);
            Assert.NotNull(result);
        }

        [Fact]
        public async Task UpdateFundManagers_ShouldHandleException()
        {
            // Arrange
            var fundCode = "123456";

            _mockManagerRepository.Setup(r => r.AddAsync(It.IsAny<FundManager>()))
                .ThrowsAsync(new Exception("Add failed"));

            // Act & Assert
            var result = await _fundDataService.UpdateFundManagers(fundCode);
            Assert.NotNull(result);
        }

        [Fact]
        public void FundDataService_Constructor_ShouldThrowArgumentNullExceptionWhenDependenciesAreNull()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new FundDataService(
                null,
                _mockNavHistoryRepository.Object,
                _mockPerformanceRepository.Object,
                _mockManagerRepository.Object,
                _mockAssetScaleRepository.Object,
                _mockPurchaseStatusRepository.Object,
                _mockRedemptionStatusRepository.Object,
                _mockCorporateActionsRepository.Object
            ));

            Assert.Throws<ArgumentNullException>(() => new FundDataService(
                _mockFundRepository.Object,
                null,
                _mockPerformanceRepository.Object,
                _mockManagerRepository.Object,
                _mockAssetScaleRepository.Object,
                _mockPurchaseStatusRepository.Object,
                _mockRedemptionStatusRepository.Object,
                _mockCorporateActionsRepository.Object
            ));

            Assert.Throws<ArgumentNullException>(() => new FundDataService(
                _mockFundRepository.Object,
                _mockNavHistoryRepository.Object,
                null,
                _mockManagerRepository.Object,
                _mockAssetScaleRepository.Object,
                _mockPurchaseStatusRepository.Object,
                _mockRedemptionStatusRepository.Object,
                _mockCorporateActionsRepository.Object
            ));

            Assert.Throws<ArgumentNullException>(() => new FundDataService(
                _mockFundRepository.Object,
                _mockNavHistoryRepository.Object,
                _mockPerformanceRepository.Object,
                null,
                _mockAssetScaleRepository.Object,
                _mockPurchaseStatusRepository.Object,
                _mockRedemptionStatusRepository.Object,
                _mockCorporateActionsRepository.Object
            ));

            Assert.Throws<ArgumentNullException>(() => new FundDataService(
                _mockFundRepository.Object,
                _mockNavHistoryRepository.Object,
                _mockPerformanceRepository.Object,
                _mockManagerRepository.Object,
                null,
                _mockPurchaseStatusRepository.Object,
                _mockRedemptionStatusRepository.Object,
                _mockCorporateActionsRepository.Object
            ));

            Assert.Throws<ArgumentNullException>(() => new FundDataService(
                _mockFundRepository.Object,
                _mockNavHistoryRepository.Object,
                _mockPerformanceRepository.Object,
                _mockManagerRepository.Object,
                _mockAssetScaleRepository.Object,
                null,
                _mockRedemptionStatusRepository.Object,
                _mockCorporateActionsRepository.Object
            ));

            Assert.Throws<ArgumentNullException>(() => new FundDataService(
                _mockFundRepository.Object,
                _mockNavHistoryRepository.Object,
                _mockPerformanceRepository.Object,
                _mockManagerRepository.Object,
                _mockAssetScaleRepository.Object,
                _mockPurchaseStatusRepository.Object,
                null,
                _mockCorporateActionsRepository.Object
            ));

            Assert.Throws<ArgumentNullException>(() => new FundDataService(
                _mockFundRepository.Object,
                _mockNavHistoryRepository.Object,
                _mockPerformanceRepository.Object,
                _mockManagerRepository.Object,
                _mockAssetScaleRepository.Object,
                _mockPurchaseStatusRepository.Object,
                _mockRedemptionStatusRepository.Object,
                null
            ));
        }
    }
}
