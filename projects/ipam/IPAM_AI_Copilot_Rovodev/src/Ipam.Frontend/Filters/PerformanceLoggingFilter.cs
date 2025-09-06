using Microsoft.AspNetCore.Mvc.Filters;
using Ipam.DataAccess.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Ipam.Frontend.Filters
{
    /// <summary>
    /// Action filter for automatic performance monitoring of API endpoints
    /// </summary>
    /// <remarks>
    /// Author: IPAM Team
    /// Date: 2024-01-20
    /// </remarks>
    public class PerformanceLoggingFilter : IAsyncActionFilter
    {
        private readonly PerformanceMonitoringService _performanceService;

        public PerformanceLoggingFilter(PerformanceMonitoringService performanceService)
        {
            _performanceService = performanceService;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var stopwatch = Stopwatch.StartNew();
            var actionName = $"{context.Controller.GetType().Name}.{context.ActionDescriptor.DisplayName}";
            
            var tags = new Dictionary<string, object>
            {
                ["Controller"] = context.Controller.GetType().Name,
                ["Action"] = context.ActionDescriptor.DisplayName,
                ["HttpMethod"] = context.HttpContext.Request.Method,
                ["UserId"] = context.HttpContext.User?.Identity?.Name ?? "Anonymous"
            };

            Exception exception = null;
            try
            {
                var result = await next();
                stopwatch.Stop();

                // Record success metrics
                _performanceService.RecordMetric(
                    $"API.{actionName}", 
                    stopwatch.ElapsedMilliseconds, 
                    true, 
                    tags);

                // Record HTTP status code specific metrics
                if (result.Result != null)
                {
                    var statusCode = context.HttpContext.Response.StatusCode;
                    tags["StatusCode"] = statusCode;
                    
                    _performanceService.RecordMetric(
                        $"API.StatusCode.{statusCode}", 
                        stopwatch.ElapsedMilliseconds, 
                        statusCode < 400, 
                        tags);
                }
            }
            catch (Exception ex)
            {
                exception = ex;
                stopwatch.Stop();

                // Record failure metrics
                tags["ExceptionType"] = ex.GetType().Name;
                _performanceService.RecordMetric(
                    $"API.{actionName}", 
                    stopwatch.ElapsedMilliseconds, 
                    false, 
                    tags);

                throw; // Re-throw the exception
            }
        }
    }
}