using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Security.Claims;

namespace Ipam.Frontend.Tests.TestHelpers
{
    /// <summary>
    /// Base class for controller tests providing common setup and utilities
    /// </summary>
    /// <typeparam name="TController">The controller type being tested</typeparam>
    public abstract class ControllerTestBase<TController> : IDisposable
        where TController : ControllerBase
    {
        protected TController Controller { get; }
        protected Mock<ILogger<TController>> LoggerMock { get; }
        
        protected ControllerTestBase()
        {
            LoggerMock = new Mock<ILogger<TController>>();
            Controller = CreateController();
            SetupControllerContext();
        }
        
        /// <summary>
        /// Creates the controller instance with mocked dependencies
        /// </summary>
        protected abstract TController CreateController();
        
        /// <summary>
        /// Sets up the controller context with HTTP context and user claims
        /// </summary>
        private void SetupControllerContext()
        {
            var httpContext = new DefaultHttpContext();
            
            // Set up a default user for testing
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "test-user-id"),
                new Claim(ClaimTypes.Name, "testuser")
            };
            var identity = new ClaimsIdentity(claims, "Test");
            var principal = new ClaimsPrincipal(identity);
            
            httpContext.User = principal;
            
            Controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };
        }
        
        /// <summary>
        /// Sets up an anonymous user context for testing
        /// </summary>
        protected void SetupAnonymousUser()
        {
            var httpContext = new DefaultHttpContext();
            var identity = new ClaimsIdentity(); // No authentication
            var principal = new ClaimsPrincipal(identity);
            
            httpContext.User = principal;
            
            Controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };
        }
        
        /// <summary>
        /// Sets up a custom user context with specific claims
        /// </summary>
        protected void SetupUserContext(string userId, string userName, params Claim[] additionalClaims)
        {
            var httpContext = new DefaultHttpContext();
            
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Name, userName)
            };
            
            if (additionalClaims != null)
            {
                claims.AddRange(additionalClaims);
            }
            
            var identity = new ClaimsIdentity(claims, "Test");
            var principal = new ClaimsPrincipal(identity);
            
            httpContext.User = principal;
            
            Controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };
        }
        
        /// <summary>
        /// Cleanup method called after each test
        /// </summary>
        public virtual void Dispose()
        {
            // Override in derived classes if cleanup is needed
        }
    }
    
    /// <summary>
    /// Test constants for Frontend tests
    /// </summary>
    public static class FrontendTestConstants
    {
        public const string DefaultAddressSpaceId = "test-space";
        public const string DefaultUserId = "test-user-id";
        public const string DefaultUserName = "testuser";
        
        public static class TestRoutes
        {
            public const string TagsController = "/api/v1/{addressSpaceId}/tags";
            public const string AddressSpacesController = "/api/v1/address-spaces";
            public const string IpAllocationsController = "/api/v1/{addressSpaceId}/ip-allocations";
        }
    }
}