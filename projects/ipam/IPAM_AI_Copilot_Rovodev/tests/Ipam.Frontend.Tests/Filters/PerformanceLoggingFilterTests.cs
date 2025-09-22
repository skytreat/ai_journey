using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Ipam.Frontend.Filters;
using Ipam.DataAccess.Services;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Ipam.Frontend.Tests.Filters
{
    /// <summary>
    /// Comprehensive unit tests for PerformanceLoggingFilter
    /// </summary>
    /// <remarks>
    /// Author: IPAM Team
    /// Date: 2024-01-20
    /// </remarks>
    public class PerformanceLoggingFilterTests
    {
        private readonly Mock<PerformanceMonitoringService> _performanceServiceMock;
        private readonly PerformanceLoggingFilter _filter;

        public PerformanceLoggingFilterTests()
        {
            _performanceServiceMock = new Mock<PerformanceMonitoringService>();
            _filter = new PerformanceLoggingFilter(_performanceServiceMock.Object);
        }

        [Fact]
        public async Task OnActionExecutionAsync_SuccessfulAction_RecordsSuccessMetric()
        {
            // Arrange
            var context = CreateActionExecutingContext("TestController", "TestAction", "GET");
            var next = CreateActionExecutionDelegate(context, successful: true);

            // Act
            await _filter.OnActionExecutionAsync(context, next);

            // Assert
            _performanceServiceMock.Verify(x => x.RecordMetric(
                "API.TestController.TestAction",
                It.Is<double>(d => d >= 0),
                true,
                It.Is<Dictionary<string, object>>(d => 
                    d.ContainsKey("Controller") && 
                    d.ContainsKey("Action") && 
                    d.ContainsKey("HttpMethod"))),
                Times.Once);
        }

        [Fact]
        public async Task OnActionExecutionAsync_FailedAction_RecordsFailureMetric()
        {
            // Arrange
            var context = CreateActionExecutingContext("TestController", "TestAction", "POST");
            var next = CreateActionExecutionDelegate(context, successful: false, 
                exception: new InvalidOperationException("Test exception"));

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _filter.OnActionExecutionAsync(context, next));

            _performanceServiceMock.Verify(x => x.RecordMetric(
                "API.TestController.TestAction",
                It.Is<double>(d => d >= 0),
                false,
                It.Is<Dictionary<string, object>>(d => 
                    d.ContainsKey("ExceptionType") && 
                    d["ExceptionType"].ToString() == "InvalidOperationException")),
                Times.Once);
        }

        [Fact]
        public async Task OnActionExecutionAsync_WithAuthenticatedUser_IncludesUserId()
        {
            // Arrange
            var context = CreateActionExecutingContext("TestController", "TestAction", "GET", "testuser");
            var next = CreateActionExecutionDelegate(context, successful: true);

            // Act
            await _filter.OnActionExecutionAsync(context, next);

            // Assert
            _performanceServiceMock.Verify(x => x.RecordMetric(
                It.Is<string>(s => s.StartsWith("API.")),
                It.Is<double>(d => d >= 0),
                It.Is<bool>(b => b),
                It.Is<Dictionary<string, object>>(d => 
                    d.ContainsKey("UserId") && 
                    d["UserId"].ToString() == "testuser")),
                Times.Once);
        }

        [Fact]
        public async Task OnActionExecutionAsync_WithAnonymousUser_IncludesAnonymousUserId()
        {
            // Arrange
            var context = CreateActionExecutingContext("TestController", "TestAction", "GET", userId: null);
            var next = CreateActionExecutionDelegate(context, successful: true);

            // Act
            await _filter.OnActionExecutionAsync(context, next);

            // Assert
            _performanceServiceMock.Verify(x => x.RecordMetric(
                It.Is<string>(s => s.StartsWith("API.")),
                It.Is<double>(d => d >= 0),
                It.Is<bool>(b => b),
                It.Is<Dictionary<string, object>>(d => 
                    d.ContainsKey("UserId") && 
                    d["UserId"].ToString() == "Anonymous")),
                Times.Once);
        }

        [Fact]
        public async Task OnActionExecutionAsync_RecordsStatusCodeSpecificMetric()
        {
            // Arrange
            var context = CreateActionExecutingContext("TestController", "TestAction", "GET");
            var next = CreateActionExecutionDelegate(context, successful: true, statusCode: 201);

            // Act
            await _filter.OnActionExecutionAsync(context, next);

            // Assert
            _performanceServiceMock.Verify(x => x.RecordMetric(
                 "API.StatusCode.201",
                 It.Is<double>(d => d >= 0),
                 true,
                 It.Is<Dictionary<string, object>>(d => 
                     d.ContainsKey("StatusCode") && 
                     d["StatusCode"].ToString() == "201")),
                 Times.Once);
        }

        [Fact]
        public async Task OnActionExecutionAsync_4xxStatusCode_RecordsAsFailure()
        {
            // Arrange
            var context = CreateActionExecutingContext("TestController", "TestAction", "GET");
            var next = CreateActionExecutionDelegate(context, successful: true, statusCode: 400);

            // Act
            await _filter.OnActionExecutionAsync(context, next);

            // Assert
            _performanceServiceMock.Verify(x => x.RecordMetric(
                 "API.StatusCode.400",
                 It.Is<double>(d => d >= 0),
                 false, // 4xx should be recorded as failure
                 It.Is<Dictionary<string, object>>(d => 
                     d.ContainsKey("StatusCode") && 
                     d["StatusCode"].ToString() == "400")),
                 Times.Once);
        }

        [Fact]
        public async Task OnActionExecutionAsync_5xxStatusCode_RecordsAsFailure()
        {
            // Arrange
            var context = CreateActionExecutingContext("TestController", "TestAction", "GET");
            var next = CreateActionExecutionDelegate(context, successful: true, statusCode: 500);

            // Act
            await _filter.OnActionExecutionAsync(context, next);

            // Assert
            _performanceServiceMock.Verify(x => x.RecordMetric(
                "API.StatusCode.500",
                It.Is<double>(d => d >= 0),
                false, // 5xx should be recorded as failure
                It.Is<Dictionary<string, object>>(d => 
                    d.ContainsKey("StatusCode") && 
                    d["StatusCode"].ToString() == "500")),
                Times.Once);
        }

        [Theory]
        [InlineData("GET")]
        [InlineData("POST")]
        [InlineData("PUT")]
        [InlineData("DELETE")]
        [InlineData("PATCH")]
        public async Task OnActionExecutionAsync_DifferentHttpMethods_RecordsCorrectMethod(string httpMethod)
        {
            // Arrange
            var context = CreateActionExecutingContext("TestController", "TestAction", httpMethod);
            var next = CreateActionExecutionDelegate(context, successful: true);

            // Act
            await _filter.OnActionExecutionAsync(context, next);

            // Assert
            _performanceServiceMock.Verify(x => x.RecordMetric(
                It.Is<string>(s => s.StartsWith("API.")),
                It.Is<double>(d => d >= 0),
                It.Is<bool>(b => b),
                It.Is<Dictionary<string, object>>(d => 
                    d.ContainsKey("HttpMethod") && 
                    d["HttpMethod"].ToString() == httpMethod)),
                Times.Once);
        }

        [Fact]
        public async Task OnActionExecutionAsync_MeasuresExecutionTime()
        {
            // Arrange
            var context = CreateActionExecutingContext("TestController", "TestAction", "GET");
            var delay = TimeSpan.FromMilliseconds(100);
            var next = CreateActionExecutionDelegate(context, successful: true, delay: delay);

            // Act
            await _filter.OnActionExecutionAsync(context, next);

            // Assert
            _performanceServiceMock.Verify(x => x.RecordMetric(
                It.Is<string>(s => s.StartsWith("API.")),
                It.Is<double>(d => d >= delay.TotalMilliseconds),
                It.Is<bool>(b => b),
                It.Is<Dictionary<string, object>>(d => d != null)),
                Times.AtLeastOnce);
        }

        [Fact]
        public async Task OnActionExecutionAsync_NoResult_DoesNotRecordStatusCodeMetric()
        {
            // Arrange
            var context = CreateActionExecutingContext("TestController", "TestAction", "GET");
            var next = CreateActionExecutionDelegate(context, successful: true, hasResult: false);

            // Act
            await _filter.OnActionExecutionAsync(context, next);

            // Assert
            _performanceServiceMock.Verify(x => x.RecordMetric(
                "API.TestController.TestAction",
                It.Is<double>(d => d >= 0),
                true,
                It.Is<Dictionary<string, object>>(d => d != null)),
                Times.Once);

            // Should not record status code metric when no result
            _performanceServiceMock.Verify(x => x.RecordMetric(
                It.Is<string>(s => s.StartsWith("API.StatusCode.")),
                It.Is<double>(d => d >= 0),
                It.Is<bool>(b => b),
                It.Is<Dictionary<string, object>>(d => d != null)),
                Times.Never);
        }

        private static ActionExecutingContext CreateActionExecutingContext(
            string controllerName, 
            string actionName, 
            string httpMethod,
            string userId = null)
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Method = httpMethod;

            if (!string.IsNullOrEmpty(userId))
            {
                var claims = new[] { new Claim(ClaimTypes.Name, userId) };
                httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims));
            }

            var actionDescriptor = new Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor
            {
                ControllerName = controllerName,
                ActionName = actionName,
                DisplayName = actionName
            };

            var controller = new Mock<Controller>();
            controller.Object.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext,
                ActionDescriptor = actionDescriptor
            };

            return new ActionExecutingContext(
                new ActionContext(httpContext, new RouteData(), actionDescriptor),
                new List<IFilterMetadata>(),
                new Dictionary<string, object>(),
                controller.Object);
        }

        private static ActionExecutionDelegate CreateActionExecutionDelegate(
            ActionExecutingContext context,
            bool successful,
            Exception exception = null,
            int statusCode = 200,
            bool hasResult = true,
            TimeSpan? delay = null)
        {
            return async () =>
            {
                if (delay.HasValue)
                {
                    await Task.Delay(delay.Value);
                }

                if (!successful && exception != null)
                {
                    throw exception;
                }

                var result = hasResult ? new OkResult() : null;
                if (hasResult)
                {
                    context.HttpContext.Response.StatusCode = statusCode;
                }

                return new ActionExecutedContext(
                    context,
                    new List<IFilterMetadata>(),
                    context.Controller)
                {
                    Result = result,
                    Exception = exception
                };
            };
        }
    }
}