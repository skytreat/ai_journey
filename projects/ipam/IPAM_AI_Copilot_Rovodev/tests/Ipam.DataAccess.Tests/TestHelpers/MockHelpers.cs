using Microsoft.Extensions.Configuration;
using Moq;
using Ipam.DataAccess.Interfaces;
using Ipam.DataAccess.Entities;
using Ipam.DataAccess.Services;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Ipam.DataAccess.Tests.TestHelpers
{
    /// <summary>
    /// Centralized mock setup helpers to eliminate duplication across test files
    /// </summary>
    public static class MockHelpers
    {
        /// <summary>
        /// Creates a mock configuration with Azure Storage connection string
        /// </summary>
        public static Mock<IConfiguration> CreateMockConfiguration(string connectionString = TestConstants.ConnectionStrings.DefaultAzureStorage)
        {
            var configMock = new Mock<IConfiguration>();
            var connectionStringsSection = new Mock<IConfigurationSection>();
            var azureTableStorageOptions = new Mock<IConfigurationSection>();
            
            connectionStringsSection.Setup(s => s["AzureTableStorage"]).Returns(connectionString);
            azureTableStorageOptions.Setup(s => s["TableNamePrefix"]).Returns(TestConstants.ConnectionStrings.TestTablePrefix);
            
            configMock.Setup(c => c.GetSection("ConnectionStrings")).Returns(connectionStringsSection.Object);
            configMock.Setup(c => c.GetSection("AzureTableStorageOptions")).Returns(azureTableStorageOptions.Object);
            
            return configMock;
        }
        
        /// <summary>
        /// Sets up default repository mock behaviors for common scenarios
        /// </summary>
        public static void SetupDefaultRepositoryMocks(Mock<IIpAllocationRepository> repoMock, string addressSpaceId = TestConstants.DefaultAddressSpaceId)
        {
            repoMock.Setup(r => r.GetByIdAsync(addressSpaceId, It.IsAny<string>()))
                .ReturnsAsync((IpAllocationEntity?)null);
                
            repoMock.Setup(r => r.GetAllAsync(addressSpaceId))
                .ReturnsAsync(new List<IpAllocationEntity>());
                
            repoMock.Setup(r => r.CreateAsync(It.IsAny<IpAllocationEntity>()))
                .ReturnsAsync((IpAllocationEntity entity) => entity);
                
            repoMock.Setup(r => r.UpdateAsync(It.IsAny<IpAllocationEntity>()))
                .ReturnsAsync((IpAllocationEntity entity) => entity);
                
            repoMock.Setup(r => r.DeleteAsync(addressSpaceId, It.IsAny<string>()))
                .Returns(Task.CompletedTask);
        }
        
        /// <summary>
        /// Sets up tag repository mock with default behaviors
        /// </summary>
        public static void SetupDefaultTagRepositoryMocks(Mock<ITagRepository> tagRepoMock, string addressSpaceId = TestConstants.DefaultAddressSpaceId)
        {
            tagRepoMock.Setup(r => r.GetByNameAsync(addressSpaceId, It.IsAny<string>()))
                .ReturnsAsync((TagEntity?)null);
                
            tagRepoMock.Setup(r => r.GetAllAsync(addressSpaceId))
                .ReturnsAsync(new List<TagEntity>());
                
            tagRepoMock.Setup(r => r.CreateAsync(It.IsAny<TagEntity>()))
                .ReturnsAsync((TagEntity entity) => entity);
                
            tagRepoMock.Setup(r => r.UpdateAsync(It.IsAny<TagEntity>()))
                .ReturnsAsync((TagEntity entity) => entity);
                
            tagRepoMock.Setup(r => r.DeleteAsync(addressSpaceId, It.IsAny<string>()))
                .Returns(Task.CompletedTask);
        }
        
        /// <summary>
        /// Sets up address space repository mock with default behaviors
        /// </summary>
        public static void SetupDefaultAddressSpaceRepositoryMocks(Mock<IAddressSpaceRepository> repoMock)
        {
            // Note: Adjust method signatures based on actual IAddressSpaceRepository interface
            repoMock.Setup(r => r.GetByIdAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync((AddressSpaceEntity?)null);
                
            repoMock.Setup(r => r.GetAllAsync(It.IsAny<string>()))
                .ReturnsAsync(new List<AddressSpaceEntity>());
                
            repoMock.Setup(r => r.CreateAsync(It.IsAny<AddressSpaceEntity>()))
                .ReturnsAsync((AddressSpaceEntity entity) => entity);
                
            repoMock.Setup(r => r.UpdateAsync(It.IsAny<AddressSpaceEntity>()))
                .ReturnsAsync((AddressSpaceEntity entity) => entity);
                
            repoMock.Setup(r => r.DeleteAsync(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);
        }
        
        /// <summary>
        /// Creates a mock tag inheritance service with standard setup
        /// </summary>
        public static Mock<ITagRepository> CreateMockTagRepository()
        {
            var mock = new Mock<ITagRepository>();
            SetupDefaultTagRepositoryMocks(mock);
            return mock;
        }
        
        /// <summary>
        /// Sets up performance service mocks for testing
        /// </summary>
        public static void SetupPerformanceServiceMocks(Mock<IPerformanceMonitoringService> perfMock)
        {
            perfMock.Setup(p => p.RecordMetric(It.IsAny<string>(), It.IsAny<double>(), It.IsAny<bool>(), It.IsAny<Dictionary<string, object>>()));
            perfMock.Setup(p => p.GetAllStatistics()).Returns(new Dictionary<string, PerformanceStatistics>());
            perfMock.Setup(p => p.GetStatistics(It.IsAny<string>())).Returns((PerformanceStatistics?)null);
        }
    }
}