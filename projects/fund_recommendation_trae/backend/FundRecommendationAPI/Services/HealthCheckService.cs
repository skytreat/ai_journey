using System;
using System.Threading;
using System.Threading.Tasks;

namespace FundRecommendationAPI.Services
{
    public interface IHealthCheckService
    {
        Task<HealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken = default);
    }

    public class HealthCheckResult
    {
        public string Status { get; set; } = "Healthy";
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public TimeSpan ResponseTime { get; set; }
        public DatabaseHealthCheck Database { get; set; } = new();
        public MemoryHealthCheck Memory { get; set; } = new();
        public CacheHealthCheck Cache { get; set; } = new();
    }

    public class DatabaseHealthCheck
    {
        public string Status { get; set; } = "Healthy";
        public long ResponseTimeMs { get; set; }
        public string? Error { get; set; }
    }

    public class MemoryHealthCheck
    {
        public string Status { get; set; } = "Healthy";
        public long UsedMemoryMb { get; set; }
        public long TotalMemoryMb { get; set; }
        public double UsagePercent { get; set; }
    }

    public class CacheHealthCheck
    {
        public string Status { get; set; } = "Healthy";
        public int HitCount { get; set; }
        public int MissCount { get; set; }
        public double HitRate => (HitCount + MissCount) > 0 
            ? (double)HitCount / (HitCount + MissCount) * 100 
            : 0;
    }

    public class HealthCheckService : IHealthCheckService
    {
        private readonly IServiceProvider _serviceProvider;

        public HealthCheckService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken = default)
        {
            var result = new HealthCheckResult();
            var overallHealthy = true;

            var dbTask = CheckDatabaseAsync(cancellationToken);
            var memTask = CheckMemoryAsync();

            await Task.WhenAll(dbTask, memTask);

            result.Database = dbTask.Result;
            result.Memory = memTask.Result;

            if (result.Database.Status != "Healthy")
            {
                overallHealthy = false;
            }

            if (result.Memory.UsagePercent > 90)
            {
                result.Memory.Status = "Degraded";
                overallHealthy = false;
            }

            result.Status = overallHealthy ? "Healthy" : "Unhealthy";

            return result;
        }

        private async Task<DatabaseHealthCheck> CheckDatabaseAsync(CancellationToken cancellationToken)
        {
            var check = new DatabaseHealthCheck();
            var sw = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetService<FundRecommendationAPI.Models.FundDbContext>();

                if (dbContext != null)
                {
                    var canConnect = await dbContext.Database.CanConnectAsync(cancellationToken);
                    if (canConnect)
                    {
                        check.Status = "Healthy";
                    }
                    else
                    {
                        check.Status = "Unhealthy";
                        check.Error = "Cannot connect to database";
                    }
                }
                else
                {
                    check.Status = "Degraded";
                    check.Error = "Database context not available";
                }
            }
            catch (Exception ex)
            {
                check.Status = "Unhealthy";
                check.Error = ex.Message;
            }
            finally
            {
                sw.Stop();
                check.ResponseTimeMs = sw.ElapsedMilliseconds;
            }

            return check;
        }

        private Task<MemoryHealthCheck> CheckMemoryAsync()
        {
            var check = new MemoryHealthCheck();

            try
            {
                var process = System.Diagnostics.Process.GetCurrentProcess();
                check.UsedMemoryMb = process.WorkingSet64 / 1024 / 1024;
                check.TotalMemoryMb = 8192;
                check.UsagePercent = (double)check.UsedMemoryMb / check.TotalMemoryMb * 100;
                check.Status = check.UsagePercent < 90 ? "Healthy" : "Degraded";
            }
            catch
            {
                check.Status = "Unknown";
            }

            return Task.FromResult(check);
        }
    }
}
