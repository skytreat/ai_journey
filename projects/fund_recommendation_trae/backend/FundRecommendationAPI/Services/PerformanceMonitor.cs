using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FundRecommendationAPI.Services
{
    public interface IPerformanceMonitor
    {
        void RecordRequest(string endpoint, long durationMs, int statusCode);
        object GetMetrics();
        void Reset();
    }

    public class PerformanceMonitor : IPerformanceMonitor
    {
        private readonly ConcurrentDictionary<string, List<long>> _requestDurations = new();
        private readonly ConcurrentDictionary<string, RequestMetrics> _metrics = new();
        private long _totalRequests;
        private long _totalErrors;

        public void RecordRequest(string endpoint, long durationMs, int statusCode)
        {
            Interlocked.Increment(ref _totalRequests);

            if (statusCode >= 400)
            {
                Interlocked.Increment(ref _totalErrors);
            }

            _requestDurations.AddOrUpdate(
                endpoint,
                _ => new List<long> { durationMs },
                (_, list) =>
                {
                    lock (list)
                    {
                        list.Add(durationMs);
                        if (list.Count > 1000)
                        {
                            list.RemoveAt(0);
                        }
                        return list;
                    }
                });

            _metrics.AddOrUpdate(
                endpoint,
                _ => new RequestMetrics
                {
                    Endpoint = endpoint,
                    RequestCount = 1,
                    ErrorCount = statusCode >= 400 ? 1 : 0,
                    MinDuration = durationMs,
                    MaxDuration = durationMs,
                    LastRequestTime = DateTime.UtcNow
                },
                (_, m) =>
                {
                    m.RequestCount++;
                    if (statusCode >= 400) m.ErrorCount++;
                    if (durationMs < m.MinDuration) m.MinDuration = durationMs;
                    if (durationMs > m.MaxDuration) m.MaxDuration = durationMs;
                    m.LastRequestTime = DateTime.UtcNow;
                    return m;
                });
        }

        public object GetMetrics()
        {
            var endpointMetrics = _metrics.Values.Select(m =>
            {
                var durations = _requestDurations.GetValueOrDefault(m.Endpoint, new List<long>());
                lock (durations)
                {
                    var sorted = durations.OrderBy(d => d).ToList();
                    return new
                    {
                        m.Endpoint,
                        m.RequestCount,
                        m.ErrorCount,
                        ErrorRate = m.RequestCount > 0 ? (double)m.ErrorCount / m.RequestCount : 0,
                        m.MinDuration,
                        m.MaxDuration,
                        AvgDuration = sorted.Any() ? sorted.Average() : 0,
                        P50Duration = sorted.Any() ? sorted[sorted.Count / 2] : 0,
                        P95Duration = sorted.Any() ? sorted[(int)(sorted.Count * 0.95)] : 0,
                        P99Duration = sorted.Any() ? sorted[(int)(sorted.Count * 0.99)] : 0,
                        m.LastRequestTime
                    };
                }
            }).ToList();

            return new
            {
                TotalRequests = _totalRequests,
                TotalErrors = _totalErrors,
                OverallErrorRate = _totalRequests > 0 ? (double)_totalErrors / _totalRequests : 0,
                Endpoints = endpointMetrics
            };
        }

        public void Reset()
        {
            _requestDurations.Clear();
            _metrics.Clear();
            Interlocked.Exchange(ref _totalRequests, 0);
            Interlocked.Exchange(ref _totalErrors, 0);
        }

        private class RequestMetrics
        {
            public string Endpoint { get; set; } = string.Empty;
            public long RequestCount { get; set; }
            public long ErrorCount { get; set; }
            public long MinDuration { get; set; } = long.MaxValue;
            public long MaxDuration { get; set; }
            public DateTime LastRequestTime { get; set; }
        }
    }
}
