using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Ipam.DataAccess.Exceptions;
using System.ComponentModel.DataAnnotations;
using Azure;

namespace Ipam.Frontend.Middleware
{
    /// <summary>
    /// Global error handling middleware for consistent error responses
    /// </summary>
    /// <remarks>
    /// Author: IPAM Team
    /// Date: 2024-01-20
    /// </remarks>
    public class GlobalErrorHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalErrorHandlingMiddleware> _logger;

        public GlobalErrorHandlingMiddleware(RequestDelegate next, ILogger<GlobalErrorHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unhandled exception occurred while processing the request");
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";

            var response = new ErrorResponse();

            switch (exception)
            {
                case ValidationException validationEx:
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    response.Message = "Validation failed";
                    response.Details = validationEx.Message;
                    break;

                case ArgumentNullException argNullEx:
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    response.Message = "Required parameter is missing";
                    response.Details = argNullEx.ParamName ?? "Unknown parameter";
                    break;

                case ArgumentException argEx:
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    response.Message = "Invalid argument";
                    response.Details = argEx.Message;
                    break;

                case IpamDataException ipamEx:
                    response.StatusCode = (int)HttpStatusCode.UnprocessableEntity;
                    response.Message = "Data operation failed";
                    response.Details = ipamEx.Message;
                    break;

                case RequestFailedException azureEx when azureEx.Status == 409:
                    response.StatusCode = (int)HttpStatusCode.Conflict;
                    response.Message = "Resource conflict";
                    response.Details = "The resource has been modified by another process. Please refresh and try again.";
                    break;

                case RequestFailedException azureEx when azureEx.Status == 404:
                    response.StatusCode = (int)HttpStatusCode.NotFound;
                    response.Message = "Resource not found";
                    response.Details = azureEx.Message;
                    break;

                case RequestFailedException azureEx when azureEx.Status == 412:
                    response.StatusCode = (int)HttpStatusCode.PreconditionFailed;
                    response.Message = "Concurrency conflict";
                    response.Details = "The resource has been modified. Please refresh and try again.";
                    break;

                case InvalidOperationException invalidOpEx:
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    response.Message = "Invalid operation";
                    response.Details = invalidOpEx.Message;
                    break;

                case UnauthorizedAccessException:
                    response.StatusCode = (int)HttpStatusCode.Forbidden;
                    response.Message = "Access denied";
                    response.Details = "You don't have permission to perform this operation";
                    break;

                case TimeoutException:
                    response.StatusCode = (int)HttpStatusCode.RequestTimeout;
                    response.Message = "Request timeout";
                    response.Details = "The operation took too long to complete";
                    break;

                default:
                    response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    response.Message = "An internal server error occurred";
                    response.Details = "Please try again later or contact support if the problem persists";
                    
                    // Log full exception details for internal server errors
                    _logger.LogError(exception, "Unhandled exception: {ExceptionType} - {Message}", 
                        exception.GetType().Name, exception.Message);
                    break;
            }

            context.Response.StatusCode = response.StatusCode;

            var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await context.Response.WriteAsync(jsonResponse);
        }
    }

    /// <summary>
    /// Standard error response model
    /// </summary>
    public class ErrorResponse
    {
        public int StatusCode { get; set; }
        public string Message { get; set; }
        public string Details { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string TraceId { get; set; } = System.Diagnostics.Activity.Current?.Id ?? Guid.NewGuid().ToString();
    }
}