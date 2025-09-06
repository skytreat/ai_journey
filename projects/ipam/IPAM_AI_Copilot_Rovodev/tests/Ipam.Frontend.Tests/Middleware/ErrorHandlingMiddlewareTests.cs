using Xunit;
using Moq;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Ipam.Frontend.Middleware;
using Ipam.DataAccess.Exceptions;
using Ipam.DataAccess.Validation;
using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Ipam.Frontend.Tests.Middleware
{
    /// <summary>
    /// Comprehensive unit tests for ErrorHandlingMiddleware
    /// </summary>
    /// <remarks>
    /// Author: IPAM Team
    /// Date: 2024-01-20
    /// </remarks>
    public class ErrorHandlingMiddlewareTests
    {
        private readonly Mock<ILogger<ErrorHandlingMiddleware>> _loggerMock;
        private readonly ErrorHandlingMiddleware _middleware;
        private readonly Mock<RequestDelegate> _nextMock;

        public ErrorHandlingMiddlewareTests()
        {
            _loggerMock = new Mock<ILogger<ErrorHandlingMiddleware>>();
            _nextMock = new Mock<RequestDelegate>();
            _middleware = new ErrorHandlingMiddleware(_nextMock.Object, _loggerMock.Object);
        }

        [Fact]
        public async Task InvokeAsync_NoException_CallsNextMiddleware()
        {
            // Arrange
            var context = CreateHttpContext();
            _nextMock.Setup(x => x(context)).Returns(Task.CompletedTask);

            // Act
            await _middleware.InvokeAsync(context);

            // Assert
            _nextMock.Verify(x => x(context), Times.Once);
            Assert.Equal(200, context.Response.StatusCode); // Default status
        }

        [Fact]
        public async Task InvokeAsync_ArgumentException_Returns400BadRequest()
        {
            // Arrange
            var context = CreateHttpContext();
            var exception = new ArgumentException("Invalid argument provided");
            
            _nextMock.Setup(x => x(context)).ThrowsAsync(exception);

            // Act
            await _middleware.InvokeAsync(context);

            // Assert
            Assert.Equal(400, context.Response.StatusCode);
            Assert.Equal("application/json", context.Response.ContentType);
            
            var responseBody = GetResponseBody(context);
            var errorResponse = JsonSerializer.Deserialize<JsonElement>(responseBody);
            Assert.Equal("Invalid argument provided", errorResponse.GetProperty("message").GetString());
            Assert.Equal("ArgumentException", errorResponse.GetProperty("type").GetString());
        }

        [Fact]
        public async Task InvokeAsync_ValidationException_Returns400BadRequest()
        {
            // Arrange
            var context = CreateHttpContext();
            var exception = new ValidationException("Validation failed");
            
            _nextMock.Setup(x => x(context)).ThrowsAsync(exception);

            // Act
            await _middleware.InvokeAsync(context);

            // Assert
            Assert.Equal(400, context.Response.StatusCode);
            
            var responseBody = GetResponseBody(context);
            var errorResponse = JsonSerializer.Deserialize<JsonElement>(responseBody);
            Assert.Equal("Validation failed", errorResponse.GetProperty("message").GetString());
            Assert.Equal("ValidationException", errorResponse.GetProperty("type").GetString());
        }

        [Fact]
        public async Task InvokeAsync_EntityNotFoundException_Returns404NotFound()
        {
            // Arrange
            var context = CreateHttpContext();
            var exception = new EntityNotFoundException("Entity not found");
            
            _nextMock.Setup(x => x(context)).ThrowsAsync(exception);

            // Act
            await _middleware.InvokeAsync(context);

            // Assert
            Assert.Equal(404, context.Response.StatusCode);
            
            var responseBody = GetResponseBody(context);
            var errorResponse = JsonSerializer.Deserialize<JsonElement>(responseBody);
            Assert.Equal("Entity not found", errorResponse.GetProperty("message").GetString());
            Assert.Equal("EntityNotFoundException", errorResponse.GetProperty("type").GetString());
        }

        [Fact]
        public async Task InvokeAsync_ConcurrencyException_Returns409Conflict()
        {
            // Arrange
            var context = CreateHttpContext();
            var exception = new ConcurrencyException("Concurrency conflict detected");
            
            _nextMock.Setup(x => x(context)).ThrowsAsync(exception);

            // Act
            await _middleware.InvokeAsync(context);

            // Assert
            Assert.Equal(409, context.Response.StatusCode);
            
            var responseBody = GetResponseBody(context);
            var errorResponse = JsonSerializer.Deserialize<JsonElement>(responseBody);
            Assert.Equal("Concurrency conflict detected", errorResponse.GetProperty("message").GetString());
            Assert.Equal("ConcurrencyException", errorResponse.GetProperty("type").GetString());
        }

        [Fact]
        public async Task InvokeAsync_UnauthorizedAccessException_Returns403Forbidden()
        {
            // Arrange
            var context = CreateHttpContext();
            var exception = new UnauthorizedAccessException("Access denied");
            
            _nextMock.Setup(x => x(context)).ThrowsAsync(exception);

            // Act
            await _middleware.InvokeAsync(context);

            // Assert
            Assert.Equal(403, context.Response.StatusCode);
            
            var responseBody = GetResponseBody(context);
            var errorResponse = JsonSerializer.Deserialize<JsonElement>(responseBody);
            Assert.Equal("Access denied", errorResponse.GetProperty("message").GetString());
            Assert.Equal("UnauthorizedAccessException", errorResponse.GetProperty("type").GetString());
        }

        [Fact]
        public async Task InvokeAsync_InvalidOperationException_Returns422UnprocessableEntity()
        {
            // Arrange
            var context = CreateHttpContext();
            var exception = new InvalidOperationException("Invalid operation");
            
            _nextMock.Setup(x => x(context)).ThrowsAsync(exception);

            // Act
            await _middleware.InvokeAsync(context);

            // Assert
            Assert.Equal(422, context.Response.StatusCode);
            
            var responseBody = GetResponseBody(context);
            var errorResponse = JsonSerializer.Deserialize<JsonElement>(responseBody);
            Assert.Equal("Invalid operation", errorResponse.GetProperty("message").GetString());
            Assert.Equal("InvalidOperationException", errorResponse.GetProperty("type").GetString());
        }

        [Fact]
        public async Task InvokeAsync_GenericException_Returns500InternalServerError()
        {
            // Arrange
            var context = CreateHttpContext();
            var exception = new Exception("Unexpected error occurred");
            
            _nextMock.Setup(x => x(context)).ThrowsAsync(exception);

            // Act
            await _middleware.InvokeAsync(context);

            // Assert
            Assert.Equal(500, context.Response.StatusCode);
            
            var responseBody = GetResponseBody(context);
            var errorResponse = JsonSerializer.Deserialize<JsonElement>(responseBody);
            Assert.Equal("An internal server error occurred", errorResponse.GetProperty("message").GetString());
            Assert.Equal("Exception", errorResponse.GetProperty("type").GetString());
        }

        [Fact]
        public async Task InvokeAsync_ExceptionWithInnerException_IncludesInnerExceptionDetails()
        {
            // Arrange
            var context = CreateHttpContext();
            var innerException = new ArgumentException("Inner exception message");
            var exception = new InvalidOperationException("Outer exception message", innerException);
            
            _nextMock.Setup(x => x(context)).ThrowsAsync(exception);

            // Act
            await _middleware.InvokeAsync(context);

            // Assert
            Assert.Equal(422, context.Response.StatusCode);
            
            var responseBody = GetResponseBody(context);
            var errorResponse = JsonSerializer.Deserialize<JsonElement>(responseBody);
            Assert.Equal("Outer exception message", errorResponse.GetProperty("message").GetString());
            
            // Check if inner exception details are included
            if (errorResponse.TryGetProperty("innerException", out var innerExceptionElement))
            {
                Assert.Equal("Inner exception message", innerExceptionElement.GetProperty("message").GetString());
                Assert.Equal("ArgumentException", innerExceptionElement.GetProperty("type").GetString());
            }
        }

        [Fact]
        public async Task InvokeAsync_LogsExceptionDetails()
        {
            // Arrange
            var context = CreateHttpContext();
            var exception = new ArgumentException("Test exception for logging");
            
            _nextMock.Setup(x => x(context)).ThrowsAsync(exception);

            // Act
            await _middleware.InvokeAsync(context);

            // Assert
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Test exception for logging")),
                    exception,
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task InvokeAsync_IncludesCorrelationIdInResponse()
        {
            // Arrange
            var context = CreateHttpContext();
            var correlationId = Guid.NewGuid().ToString();
            context.Request.Headers.Add("X-Correlation-ID", correlationId);
            
            var exception = new ArgumentException("Test exception");
            _nextMock.Setup(x => x(context)).ThrowsAsync(exception);

            // Act
            await _middleware.InvokeAsync(context);

            // Assert
            var responseBody = GetResponseBody(context);
            var errorResponse = JsonSerializer.Deserialize<JsonElement>(responseBody);
            
            if (errorResponse.TryGetProperty("correlationId", out var correlationIdElement))
            {
                Assert.Equal(correlationId, correlationIdElement.GetString());
            }
        }

        [Fact]
        public async Task InvokeAsync_IncludesTimestampInResponse()
        {
            // Arrange
            var context = CreateHttpContext();
            var exception = new ArgumentException("Test exception");
            
            _nextMock.Setup(x => x(context)).ThrowsAsync(exception);

            // Act
            var beforeTime = DateTime.UtcNow;
            await _middleware.InvokeAsync(context);
            var afterTime = DateTime.UtcNow;

            // Assert
            var responseBody = GetResponseBody(context);
            var errorResponse = JsonSerializer.Deserialize<JsonElement>(responseBody);
            
            if (errorResponse.TryGetProperty("timestamp", out var timestampElement))
            {
                var timestamp = DateTime.Parse(timestampElement.GetString());
                Assert.True(timestamp >= beforeTime && timestamp <= afterTime);
            }
        }

        [Fact]
        public async Task InvokeAsync_ResponseAlreadyStarted_DoesNotModifyResponse()
        {
            // Arrange
            var context = CreateHttpContext();
            var exception = new ArgumentException("Test exception");
            
            // Simulate response already started
            context.Response.StatusCode = 200;
            await context.Response.WriteAsync("Already started");
            
            _nextMock.Setup(x => x(context)).ThrowsAsync(exception);

            // Act
            await _middleware.InvokeAsync(context);

            // Assert
            // Should not change the status code since response already started
            Assert.Equal(200, context.Response.StatusCode);
        }

        private static HttpContext CreateHttpContext()
        {
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();
            return context;
        }

        private static string GetResponseBody(HttpContext context)
        {
            context.Response.Body.Seek(0, SeekOrigin.Begin);
            using var reader = new StreamReader(context.Response.Body, Encoding.UTF8);
            return reader.ReadToEnd();
        }
    }
}