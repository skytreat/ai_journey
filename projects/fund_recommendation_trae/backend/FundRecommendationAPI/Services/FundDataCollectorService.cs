using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace FundRecommendationAPI.Services
{
    public class FundDataCollectorService : BackgroundService, IFundDataCollectorService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<FundDataCollectorService> _logger;

        public FundDataCollectorService(IServiceProvider serviceProvider, ILogger<FundDataCollectorService> logger)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Fund data collector service is starting.");

            using var scope = _serviceProvider.CreateScope();
            var systemService = scope.ServiceProvider.GetRequiredService<ISystemService>();

            await CollectFundDataAsync(systemService);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var now = DateTime.Now;
                    var nextTaskTime = GetNextExecutionTime(now, 19, 30);
                    var delay = nextTaskTime - now;

                    if (delay > TimeSpan.Zero)
                    {
                        _logger.LogInformation($"Scheduling next data collection at {nextTaskTime}");
                        await Task.Delay(delay, stoppingToken);
                    }

                    using var scopeForTask = _serviceProvider.CreateScope();
                    var systemServiceForTask = scopeForTask.ServiceProvider.GetRequiredService<ISystemService>();
                    await CollectFundDataAsync(systemServiceForTask);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Fund data collector service is stopping.");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in fund data collector service");
                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                }
            }
        }

        private async Task CollectFundDataAsync(ISystemService systemService)
        {
            try
            {
                _logger.LogInformation("Starting scheduled fund data collection");
                await systemService.TriggerDataUpdateAsync("incremental");
                _logger.LogInformation("Completed scheduled fund data collection");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to collect fund data");
            }
        }

        private DateTime GetNextExecutionTime(DateTime now, int hour, int minute)
        {
            var today = new DateTime(now.Year, now.Month, now.Day, hour, minute, 0);
            if (today <= now)
            {
                today = today.AddDays(1);
            }
            return today;
        }

        public override void Dispose()
        {
            base.Dispose();
        }
    }
}
