using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System;

namespace Ipam.DataAccess.Tests.TestHelpers
{
    /// <summary>
    /// Base class for repository tests providing common setup and utilities
    /// </summary>
    /// <typeparam name="TRepository">The repository type being tested</typeparam>
    /// <typeparam name="TEntity">The entity type handled by the repository</typeparam>
    public abstract class RepositoryTestBase<TRepository, TEntity> : IDisposable
        where TRepository : class
        where TEntity : class
    {
        protected Mock<IConfiguration> ConfigMock { get; }
        protected Mock<ILogger<TRepository>> LoggerMock { get; }
        protected TRepository Repository { get; }
        
        protected RepositoryTestBase()
        {
            ConfigMock = MockHelpers.CreateMockConfiguration();
            LoggerMock = new Mock<ILogger<TRepository>>();
            Repository = CreateRepository();
        }
        
        /// <summary>
        /// Creates the repository instance with mocked dependencies
        /// </summary>
        protected abstract TRepository CreateRepository();
        
        /// <summary>
        /// Gets the default address space ID for tests
        /// </summary>
        protected virtual string GetDefaultAddressSpaceId() => TestConstants.DefaultAddressSpaceId;
        
        /// <summary>
        /// Gets the default test entity
        /// </summary>
        protected abstract TEntity CreateTestEntity();
        
        /// <summary>
        /// Cleanup method called after each test
        /// </summary>
        public virtual void Dispose()
        {
            // Override in derived classes if cleanup is needed
        }
    }
}