using System;
using System.Threading;
using System.Threading.Tasks;
using FundRecommendationAPI.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace FundRecommendationAPI.Tests
{
    public class FundDataCollectorServiceTests
    {
        private readonly Mock<ILogger<FundDataCollectorService>> _mockLogger;
        private readonly Mock<ISystemService> _mockSystemService;
        private readonly FundDataCollectorService _service;

        public FundDataCollectorServiceTests()
        {
            _mockLogger = new Mock<ILogger<FundDataCollectorService>>();
            _mockSystemService = new Mock<ISystemService>();
            _service = new FundDataCollectorService(_mockLogger.Object, _mockSystemService.Object);
        }

        [Fact]
        public void FundDataCollectorService_Constructor_ShouldInitializeCorrectly()
        {
            var service = new FundDataCollectorService(_mockLogger.Object, _mockSystemService.Object);
            Assert.NotNull(service);
        }

        [Fact]
        public void FundDataCollectorService_Constructor_ThrowsArgumentNullException_WhenLoggerIsNull()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new FundDataCollectorService(null, _mockSystemService.Object));
        }

        [Fact]
        public void FundDataCollectorService_Constructor_ThrowsArgumentNullException_WhenSystemServiceIsNull()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new FundDataCollectorService(_mockLogger.Object, null));
        }

        [Fact]
        public void GetNextExecutionTime_ShouldReturnToday_WhenTimeInFuture()
        {
            var now = new DateTime(2024, 1, 1, 10, 0, 0);
            var hour = 15;
            var minute = 30;

            var result = GetPrivateMethod<Func<DateTime, int, int, DateTime>>(_service, "GetNextExecutionTime")(now, hour, minute);

            Assert.Equal(new DateTime(2024, 1, 1, 15, 30, 0), result);
        }

        [Fact]
        public void GetNextExecutionTime_ShouldReturnTomorrow_WhenTimeInPast()
        {
            var now = new DateTime(2024, 1, 1, 10, 0, 0);
            var hour = 5;
            var minute = 30;

            var result = GetPrivateMethod<Func<DateTime, int, int, DateTime>>(_service, "GetNextExecutionTime")(now, hour, minute);

            Assert.Equal(new DateTime(2024, 1, 2, 5, 30, 0), result);
        }

        [Fact]
        public void GetNextSundayExecutionTime_ShouldReturnNextSunday_WhenTodayIsMonday()
        {
            var now = new DateTime(2024, 1, 1, 10, 0, 0);
            var hour = 3;
            var minute = 0;

            var result = GetPrivateMethod<Func<DateTime, int, int, DateTime>>(_service, "GetNextSundayExecutionTime")(now, hour, minute);

            Assert.Equal(new DateTime(2024, 1, 7, 3, 0, 0), result);
        }

        [Fact]
        public void GetNextSundayExecutionTime_ShouldReturnNextSunday_WhenTodayIsSunday()
        {
            var now = new DateTime(2024, 1, 7, 10, 0, 0);
            var hour = 3;
            var minute = 0;

            var result = GetPrivateMethod<Func<DateTime, int, int, DateTime>>(_service, "GetNextSundayExecutionTime")(now, hour, minute);

            Assert.Equal(new DateTime(2024, 1, 14, 3, 0, 0), result);
        }

        [Fact]
        public async Task CollectFundDataAsync_ShouldCallRealDataUpdate()
        {
            _mockSystemService
                .Setup(s => s.RealDataUpdateAsync("incremental", It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            await GetPrivateMethod<Func<Task>>(_service, "CollectFundDataAsync")();

            _mockSystemService
                .Verify(s => s.RealDataUpdateAsync("incremental", It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task ExecuteScheduledTasks_ShouldRunIncrementalUpdate_At1930()
        {
            var now = new DateTime(2024, 1, 1, 19, 30, 0);
            var cancellationToken = CancellationToken.None;

            _mockSystemService
                .Setup(s => s.RealDataUpdateAsync("incremental", cancellationToken))
                .Returns(Task.CompletedTask);

            await GetPrivateMethod<Func<DateTime, CancellationToken, Task>>(_service, "ExecuteScheduledTasks")(now, cancellationToken);

            _mockSystemService
                .Verify(s => s.RealDataUpdateAsync("incremental", cancellationToken),
                Times.Once);
        }

        [Fact]
        public async Task ExecuteScheduledTasks_ShouldRunFullUpdate_OnSunday0300()
        {
            var now = new DateTime(2024, 1, 7, 3, 0, 0);
            var cancellationToken = CancellationToken.None;

            _mockSystemService
                .Setup(s => s.RealDataUpdateAsync("full", cancellationToken))
                .Returns(Task.CompletedTask);

            await GetPrivateMethod<Func<DateTime, CancellationToken, Task>>(_service, "ExecuteScheduledTasks")(now, cancellationToken);

            _mockSystemService
                .Verify(s => s.RealDataUpdateAsync("full", cancellationToken),
                Times.Once);
        }

        private T GetPrivateMethod<T>(object obj, string methodName) where T : Delegate
        {
            var method = obj.GetType().GetMethod(methodName,
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);
            return (T)Delegate.CreateDelegate(typeof(T), obj, method);
        }
    }
}