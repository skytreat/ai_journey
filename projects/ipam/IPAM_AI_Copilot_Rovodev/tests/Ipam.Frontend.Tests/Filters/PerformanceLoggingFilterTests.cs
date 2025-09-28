using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Ipam.Frontend.Filters;
using Ipam.DataAccess.Services;
using Ipam.DataAccess.Interfaces;
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
        private readonly Mock<IPerformanceMonitoringService> _performanceServiceMock;
        private readonly PerformanceLoggingFilter _filter;

        public PerformanceLoggingFilterTests()
        {
            _performanceServiceMock = new Mock<IPerformanceMonitoringService>();
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

            // Assert: At least one invocation matches expected args
            Assert.Contains(_performanceServiceMock.Invocations, inv =>
                inv.Method.Name == nameof(IPerformanceMonitoringService.RecordMetric)
                && inv.Arguments[0].ToString().StartsWith("API.")
                && (bool)inv.Arguments[2] == true
                && inv.Arguments[3] is Dictionary<string, object> dict
                && dict.ContainsKey("Controller")
                && dict.ContainsKey("Action")
                && dict.ContainsKey("HttpMethod")
            );
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

            Assert.Contains(_performanceServiceMock.Invocations, inv =>
                inv.Method.Name == nameof(IPerformanceMonitoringService.RecordMetric)
                && inv.Arguments[0].ToString().StartsWith("API.")
                && (bool)inv.Arguments[2] == false
                && inv.Arguments[3] is Dictionary<string, object> dict
                && dict.ContainsKey("ExceptionType")
                && dict["ExceptionType"].ToString() == "InvalidOperationException"
            );
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
            Assert.Contains(_performanceServiceMock.Invocations, inv =>
                inv.Method.Name == nameof(IPerformanceMonitoringService.RecordMetric)
                && inv.Arguments[0].ToString().StartsWith("API.")
                && (bool)inv.Arguments[2] == true
                && inv.Arguments[3] is Dictionary<string, object> dict
                && dict.ContainsKey("UserId")
                && dict["UserId"].ToString() == "testuser"
            );
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
            Assert.Contains(_performanceServiceMock.Invocations, inv =>
                inv.Method.Name == nameof(IPerformanceMonitoringService.RecordMetric)
                && inv.Arguments[0].ToString().StartsWith("API.")
                && (bool)inv.Arguments[2] == true
                && inv.Arguments[3] is Dictionary<string, object> dict
                && dict.ContainsKey("UserId")
                && dict["UserId"].ToString() == "Anonymous"
            );
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
            Assert.Contains(_performanceServiceMock.Invocations, inv =>
                inv.Method.Name == nameof(IPerformanceMonitoringService.RecordMetric)
                && inv.Arguments[0].ToString() == "API.StatusCode.201"
                && (bool)inv.Arguments[2] == true
                && inv.Arguments[3] is Dictionary<string, object> dict
                && dict.ContainsKey("StatusCode")
                && dict["StatusCode"].ToString() == "201"
            );
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
            Assert.Contains(_performanceServiceMock.Invocations, inv =>
                inv.Method.Name == nameof(IPerformanceMonitoringService.RecordMetric)
                && inv.Arguments[0].ToString() == "API.StatusCode.400"
                && (bool)inv.Arguments[2] == false
                && inv.Arguments[3] is Dictionary<string, object> dict
                && dict.ContainsKey("StatusCode")
                && dict["StatusCode"].ToString() == "400"
            );
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
            Assert.Contains(_performanceServiceMock.Invocations, inv =>
                inv.Method.Name == nameof(IPerformanceMonitoringService.RecordMetric)
                && inv.Arguments[0].ToString() == "API.StatusCode.500"
                && (bool)inv.Arguments[2] == false
                && inv.Arguments[3] is Dictionary<string, object> dict
                && dict.ContainsKey("StatusCode")
                && dict["StatusCode"].ToString() == "500"
            );
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
            Assert.Contains(_performanceServiceMock.Invocations, inv =>
                inv.Method.Name == nameof(IPerformanceMonitoringService.RecordMetric)
                && inv.Arguments[0].ToString().StartsWith("API.")
                && inv.Arguments[3] is Dictionary<string, object> dict
                && dict.ContainsKey("HttpMethod")
                && dict["HttpMethod"].ToString() == httpMethod
            );
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
            Assert.Contains(_performanceServiceMock.Invocations, inv =>
                inv.Method.Name == nameof(IPerformanceMonitoringService.RecordMetric)
                && inv.Arguments[0].ToString().StartsWith("API.")
                && inv.Arguments[1] is double ms && ms >= delay.TotalMilliseconds
            );
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
            Assert.Contains(_performanceServiceMock.Invocations, inv =>
                inv.Method.Name == nameof(IPerformanceMonitoringService.RecordMetric)
                && inv.Arguments[0].ToString().StartsWith("API.")
                && (bool)inv.Arguments[2] == true
            );

            // Should not record status code metric when no result
            Assert.DoesNotContain(_performanceServiceMock.Invocations, inv =>
                inv.Method.Name == nameof(IPerformanceMonitoringService.RecordMetric)
                && inv.Arguments[0].ToString().StartsWith("API.StatusCode.")
            );
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