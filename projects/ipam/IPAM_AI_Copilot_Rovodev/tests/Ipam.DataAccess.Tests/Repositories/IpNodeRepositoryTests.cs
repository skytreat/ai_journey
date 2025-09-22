using Xunit;
using Moq;
using Microsoft.Extensions.Configuration;
using Ipam.DataAccess.Repositories;
using Ipam.DataAccess.Entities;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Ipam.DataAccess.Tests.Repositories
{
    public class IpNodeRepositoryTests
    {
        private readonly Mock<IConfiguration> _configMock;
        private readonly IpAllocationRepository _repository;

        public IpNodeRepositoryTests()
        {
            _configMock = new Mock<IConfiguration>();
            _configMock.Setup(c => c["ConnectionStrings:AzureTableStorage"])
                       .Returns("UseDevelopmentStorage=true");
            
            _repository = new IpAllocationRepository(_configMock.Object);
        }

        [Fact]
        public async Task CreateAsync_ValidIpNode_ShouldSucceed()
        {
            // Arrange
            var ipNode = new IpAllocationEntity
            {
                PartitionKey = "space1",
                RowKey = "ip1",
                Prefix = "10.0.0.0/8",
                Tags = new Dictionary<string, string>
                {
                    { "Environment", "Production" }
                }
            };

            // Act
            var result = await _repository.CreateAsync(ipNode);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(ipNode.Prefix, result.Prefix);
        }

        [Theory]
        [InlineData("invalid")]
        [InlineData("256.1.2.3/24")]
        [InlineData("192.168.1.1/33")]
        public async Task CreateAsync_InvalidCidr_ShouldThrowValidationException(string cidr)
        {
            // Arrange
            var ipNode = new IpAllocationEntity { Prefix = cidr };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _repository.CreateAsync(ipNode));
        }
    }
}
