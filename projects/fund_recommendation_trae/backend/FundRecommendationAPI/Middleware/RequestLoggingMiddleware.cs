using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace FundRecommendationAPI.Middleware
{
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestLoggingMiddleware> _logger;

        public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var stopwatch = Stopwatch.StartNew();
            var requestId = Guid.NewGuid().ToString("N")[..8];
            var requestPath = context.Request.Path;
            var requestMethod = context.Request.Method;

            context.Items["RequestId"] = requestId;

            _logger.LogInformation(
                "[{RequestId}] {Method} {Path} started at {Timestamp}",
                requestId,
                requestMethod,
                requestPath,
                DateTime.UtcNow);

            try
            {
                await _next(context);

                stopwatch.Stop();

                _logger.LogInformation(
                    "[{RequestId}] {Method} {Path} completed with {StatusCode} in {ElapsedMs}ms",
                    requestId,
                    requestMethod,
                    requestPath,
                    context.Response.StatusCode,
                    stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(
                    ex,
                    "[{RequestId}] {Method} {Path} failed after {ElapsedMs}ms with error: {Message}",
                    requestId,
                    requestMethod,
                    requestPath,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);

                throw;
            }
        }
    }

    public static class RequestLoggingMiddlewareExtensions
    {
        public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<RequestLoggingMiddleware>();
        }
    }
}
