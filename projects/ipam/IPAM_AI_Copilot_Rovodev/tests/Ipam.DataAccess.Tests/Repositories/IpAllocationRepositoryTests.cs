using Xunit;
using Moq;
using Microsoft.Extensions.Configuration;
using Ipam.DataAccess.Repositories;
using Ipam.DataAccess.Entities;
using Ipam.DataAccess.Tests.TestHelpers;
using Azure.Data.Tables;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace Ipam.DataAccess.Tests.Repositories
{
    /// <summary>
    /// Unit tests for IpAllocationRepository
    /// </summary>
    /// <remarks>
    /// Author: IPAM Team
    /// Date: 2024-01-20
    /// </remarks>
    public class IpAllocationRepositoryTests : IDisposable
    {
        private readonly Mock<IConfiguration> _configMock;
        private readonly MockTableClient _mockTableClient;
        private readonly IpAllocationRepository _ipAllocationRepository;

        public IpAllocationRepositoryTests()
        {
            _configMock = MockHelpers.CreateMockConfiguration();

            _mockTableClient = new MockTableClient();
            _ipAllocationRepository = new IpAllocationRepository(_configMock.Object);
            
            // Replace the internal table client with our mock
            // Note: This would require making the table client injectable or using a factory pattern
            // For now, we'll test the logic that doesn't depend on Azure Table Storage
            }

            // Merged from IpNodeRepositoryTests.cs
            [Fact]
            public async Task CreateAsync_ValidIpNode_ShouldSucceed_MergedFromIpNodeRepositoryTests()
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
                var result = await _ipAllocationRepository.CreateAsync(ipNode);

                // Assert
                Assert.NotNull(result);
                Assert.Equal(ipNode.Prefix, result.Prefix);
            }

            [Theory]
            [InlineData("invalid")]
            [InlineData("256.1.2.3/24")]
            [InlineData("192.168.1.1/33")]
            public async Task CreateAsync_InvalidCidr_ShouldThrowValidationException_MergedFromIpNodeRepositoryTests(string cidr)
            {
                // Arrange
                var ipNode = new IpAllocationEntity { Prefix = cidr };

                // Act & Assert
                await Assert.ThrowsAsync<ArgumentException>(() => _ipAllocationRepository.CreateAsync(ipNode));
        }

        #region CreateAsync Tests

        [Fact]
        public async Task CreateAsync_ValidIpAllocation_ShouldSucceed()
        {
            // Arrange
            var ipAllocation = new IpAllocationEntity
            {
                Id = "ip-001",
                AddressSpaceId = "space1",
                Prefix = "10.0.1.0/24",
                ParentId = "ip-parent",
                Tags = new Dictionary<string, string> { { "Environment", "Test" } },
                CreatedOn = DateTime.UtcNow,
                ModifiedOn = DateTime.UtcNow,
            };

            // Act
            var result = await _ipAllocationRepository.CreateAsync(ipAllocation);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(ipAllocation.Id, result.Id);
            Assert.Equal(ipAllocation.Prefix, result.Prefix);
            Assert.Equal(ipAllocation.AddressSpaceId, result.AddressSpaceId);
        }

        [Fact]
        public async Task CreateAsync_NullIpAllocation_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => _ipAllocationRepository.CreateAsync(null));
        }

        [Fact]
        public async Task CreateAsync_EmptyId_ShouldThrowArgumentException()
        {
            // Arrange
            var ipAllocation = new IpAllocationEntity
            {
                Id = "",
                AddressSpaceId = "space1",
                Prefix = "10.0.1.0/24"
            };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _ipAllocationRepository.CreateAsync(ipAllocation));
        }

        [Fact]
        public async Task CreateAsync_EmptyAddressSpaceId_ShouldThrowArgumentException()
        {
            // Arrange
            var ipAllocation = new IpAllocationEntity
            {
                Id = "ip-001",
                AddressSpaceId = "",
                Prefix = "10.0.1.0/24"
            };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _ipAllocationRepository.CreateAsync(ipAllocation));
        }

        [Fact]
        public async Task CreateAsync_InvalidPrefix_ShouldThrowArgumentException()
        {
            // Arrange
            var ipAllocation = new IpAllocationEntity
            {
                Id = "ip-001",
                AddressSpaceId = "space1",
                Prefix = "invalid-cidr"
            };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _ipAllocationRepository.CreateAsync(ipAllocation));
        }

        #endregion

        #region GetByIdAsync Tests

        [Fact]
        public async Task GetByIdAsync_ExistingId_ShouldReturnIpAllocation()
        {
            // Arrange
            var addressSpaceId = "space1";
            var ipId = "ip-001";
            
            var expectedEntity = new IpAllocationEntity
            {
                Id = ipId,
                AddressSpaceId = addressSpaceId,
                Prefix = "10.0.1.0/24",
            };

            // First create the entity
            await _ipAllocationRepository.CreateAsync(expectedEntity);

            // Act
            var result = await _ipAllocationRepository.GetByIdAsync(addressSpaceId, ipId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(ipId, result.Id);
            Assert.Equal(addressSpaceId, result.AddressSpaceId);
            Assert.Equal("10.0.1.0/24", result.Prefix);
        }

        [Fact]
        public async Task GetByIdAsync_NonExistingId_ShouldReturnNull()
        {
            // Arrange
            var addressSpaceId = "space1";
            var nonExistingId = "non-existing";

            // Act
            var result = await _ipAllocationRepository.GetByIdAsync(addressSpaceId, nonExistingId);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetByIdAsync_EmptyAddressSpaceId_ShouldThrowArgumentException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _ipAllocationRepository.GetByIdAsync("", "ip-001"));
        }

        [Fact]
        public async Task GetByIdAsync_EmptyIpId_ShouldThrowArgumentException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _ipAllocationRepository.GetByIdAsync("space1", ""));
        }

        #endregion

        #region GetByPrefixAsync Tests

        [Fact]
        public async Task GetByPrefixAsync_ExistingPrefix_ShouldReturnMatchingAllocations()
        {
            // Arrange
            var addressSpaceId = "space1";
            var prefix = "10.0.1.0/24";

            var allocation1 = new IpAllocationEntity
            {
                Id = "ip-001",
                AddressSpaceId = addressSpaceId,
                Prefix = prefix,
            };

            var allocation2 = new IpAllocationEntity
            {
                Id = "ip-002",
                AddressSpaceId = addressSpaceId,
                Prefix = "10.0.2.0/24", // Different prefix
            };

            await _ipAllocationRepository.CreateAsync(allocation1);
            await _ipAllocationRepository.CreateAsync(allocation2);

            // Act
            var result = await _ipAllocationRepository.GetByPrefixAsync(addressSpaceId, prefix);

            // Assert
            Assert.NotNull(result);
            var allocations = result.ToList();
            Assert.Single(allocations);
            Assert.Equal("ip-001", allocations[0].Id);
            Assert.Equal(prefix, allocations[0].Prefix);
        }

        [Fact]
        public async Task GetByPrefixAsync_NonExistingPrefix_ShouldReturnEmptyCollection()
        {
            // Arrange
            var addressSpaceId = "space1";
            var nonExistingPrefix = "192.168.1.0/24";

            // Act
            var result = await _ipAllocationRepository.GetByPrefixAsync(addressSpaceId, nonExistingPrefix);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetByPrefixAsync_InvalidPrefix_ShouldThrowArgumentException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                _ipAllocationRepository.GetByPrefixAsync("space1", "invalid-cidr"));
        }

        #endregion

        #region GetByTagsAsync Tests

        [Fact]
        public async Task GetByTagsAsync_MatchingTags_ShouldReturnMatchingAllocations()
        {
            // Arrange
            var addressSpaceId = "space1";
            var searchTags = new Dictionary<string, string>
            {
                { "Environment", "Production" },
                { "Region", "US-East" }
            };

            var allocation1 = new IpAllocationEntity
            {
                Id = "ip-001",
                AddressSpaceId = addressSpaceId,
                Prefix = "10.0.1.0/24",
                Tags = new Dictionary<string, string>
                {
                    { "Environment", "Production" },
                    { "Region", "US-East" },
                    { "Team", "Backend" }
                }
            };

            var allocation2 = new IpAllocationEntity
            {
                Id = "ip-002",
                AddressSpaceId = addressSpaceId,
                Prefix = "10.0.2.0/24",
                Tags = new Dictionary<string, string>
                {
                    { "Environment", "Staging" }, // Different environment
                    { "Region", "US-East" }
                }
            };

            var allocation3 = new IpAllocationEntity
            {
                Id = "ip-003",
                AddressSpaceId = addressSpaceId,
                Prefix = "10.0.3.0/24",
                Tags = new Dictionary<string, string>
                {
                    { "Environment", "Production" },
                    { "Region", "US-East" }
                }
            };

            await _ipAllocationRepository.CreateAsync(allocation1);
            await _ipAllocationRepository.CreateAsync(allocation2);
            await _ipAllocationRepository.CreateAsync(allocation3);

            // Act
            var result = await _ipAllocationRepository.GetByTagsAsync(addressSpaceId, searchTags);

            // Assert
            Assert.NotNull(result);
            var allocations = result.ToList();
            Assert.Equal(2, allocations.Count);
            Assert.Contains(allocations, a => a.Id == "ip-001");
            Assert.Contains(allocations, a => a.Id == "ip-003");
            Assert.DoesNotContain(allocations, a => a.Id == "ip-002");
        }

        [Fact]
        public async Task GetByTagsAsync_EmptyTags_ShouldReturnAllAllocations()
        {
            // Arrange
            var addressSpaceId = "space1";
            var emptyTags = new Dictionary<string, string>();

            var allocation1 = new IpAllocationEntity
            {
                Id = "ip-001",
                AddressSpaceId = addressSpaceId,
                Prefix = "10.0.1.0/24"
            };

            await _ipAllocationRepository.CreateAsync(allocation1);

            // Act
            var result = await _ipAllocationRepository.GetByTagsAsync(addressSpaceId, emptyTags);

            // Assert
            Assert.NotNull(result);
            var allocations = result.ToList();
            Assert.Single(allocations);
        }

        [Fact]
        public async Task GetByTagsAsync_NullTags_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => 
                _ipAllocationRepository.GetByTagsAsync("space1", null));
        }

        #endregion

        #region GetChildrenAsync Tests

        [Fact]
        public async Task GetChildrenAsync_ExistingParent_ShouldReturnChildren()
        {
            // Arrange
            var addressSpaceId = "space1";
            var parentId = "parent-ip";

            var parent = new IpAllocationEntity
            {
                Id = parentId,
                AddressSpaceId = addressSpaceId,
                Prefix = "10.0.0.0/16",
                ChildrenIds = new List<string> { "child-1", "child-2" }
            };

            var child1 = new IpAllocationEntity
            {
                Id = "child-1",
                AddressSpaceId = addressSpaceId,
                Prefix = "10.0.1.0/24",
                ParentId = parentId
            };

            var child2 = new IpAllocationEntity
            {
                Id = "child-2",
                AddressSpaceId = addressSpaceId,
                Prefix = "10.0.2.0/24",
                ParentId = parentId
            };

            var unrelatedChild = new IpAllocationEntity
            {
                Id = "unrelated",
                AddressSpaceId = addressSpaceId,
                Prefix = "192.168.1.0/24",
                ParentId = "other-parent"
            };

            await _ipAllocationRepository.CreateAsync(parent);
            await _ipAllocationRepository.CreateAsync(child1);
            await _ipAllocationRepository.CreateAsync(child2);
            await _ipAllocationRepository.CreateAsync(unrelatedChild);

            // Act
            var result = await _ipAllocationRepository.GetChildrenAsync(addressSpaceId, parentId);

            // Assert
            Assert.NotNull(result);
            var children = result.ToList();
            Assert.Equal(2, children.Count);
            Assert.Contains(children, c => c.Id == "child-1");
            Assert.Contains(children, c => c.Id == "child-2");
            Assert.DoesNotContain(children, c => c.Id == "unrelated");
        }

        [Fact]
        public async Task GetChildrenAsync_NonExistingParent_ShouldReturnEmptyCollection()
        {
            // Arrange
            var addressSpaceId = "space1";
            var nonExistingParentId = "non-existing-parent";

            // Act
            var result = await _ipAllocationRepository.GetChildrenAsync(addressSpaceId, nonExistingParentId);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetChildrenAsync_NullParentId_ShouldReturnRootNodes()
        {
            // Arrange
            var addressSpaceId = "space1";

            var rootNode = new IpAllocationEntity
            {
                Id = "root-1",
                AddressSpaceId = addressSpaceId,
                Prefix = "10.0.0.0/8",
                ParentId = null
            };

            var childNode = new IpAllocationEntity
            {
                Id = "child-1",
                AddressSpaceId = addressSpaceId,
                Prefix = "10.0.1.0/24",
                ParentId = "root-1"
            };

            await _ipAllocationRepository.CreateAsync(rootNode);
            await _ipAllocationRepository.CreateAsync(childNode);

            // Act
            var result = await _ipAllocationRepository.GetChildrenAsync(addressSpaceId, null);

            // Assert
            Assert.NotNull(result);
            var nodes = result.ToList();
            Assert.Single(nodes);
            Assert.Equal("root-1", nodes[0].Id);
        }

        #endregion

        #region UpdateAsync Tests

        [Fact]
        public async Task UpdateAsync_ExistingIpAllocation_ShouldUpdateSuccessfully()
        {
            // Arrange
            var originalEntity = new IpAllocationEntity
            {
                Id = "ip-001",
                AddressSpaceId = "space1",
                Prefix = "10.0.1.0/24",
                Tags = new Dictionary<string, string> { { "Environment", "Test" } }
            };

            await _ipAllocationRepository.CreateAsync(originalEntity);

            // Modify the entity
            originalEntity.Tags["Environment"] = "Production";
            originalEntity.Tags.Add("Region", "US-East");
            originalEntity.ModifiedOn = DateTime.UtcNow;

            // Act
            var result = await _ipAllocationRepository.UpdateAsync(originalEntity);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Production", result.Tags["Environment"]);
            Assert.Equal("US-East", result.Tags["Region"]);
        }

        [Fact]
        public async Task UpdateAsync_NullIpAllocation_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => _ipAllocationRepository.UpdateAsync(null));
        }

        #endregion

        #region DeleteAsync Tests

        [Fact]
        public async Task DeleteAsync_ExistingIpAllocation_ShouldDeleteSuccessfully()
        {
            // Arrange
            var addressSpaceId = "space1";
            var ipId = "ip-001";

            var entity = new IpAllocationEntity
            {
                Id = ipId,
                AddressSpaceId = addressSpaceId,
                Prefix = "10.0.1.0/24"
            };

            await _ipAllocationRepository.CreateAsync(entity);

            // Act
            await _ipAllocationRepository.DeleteAsync(addressSpaceId, ipId);

            // Verify deletion
            var deletedEntity = await _ipAllocationRepository.GetByIdAsync(addressSpaceId, ipId);
            Assert.Null(deletedEntity);
        }

        [Fact]
        public async Task DeleteAsync_NonExistingIpAllocation_ShouldNotThrow()
        {
            // Arrange
            var addressSpaceId = "space1";
            var nonExistingId = "non-existing";

            // Act & Assert - Should not throw
            await _ipAllocationRepository.DeleteAsync(addressSpaceId, nonExistingId);
        }

        [Fact]
        public async Task DeleteAsync_EmptyAddressSpaceId_ShouldThrowArgumentException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _ipAllocationRepository.DeleteAsync("", "ip-001"));
        }

        [Fact]
        public async Task DeleteAsync_EmptyIpId_ShouldThrowArgumentException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _ipAllocationRepository.DeleteAsync("space1", ""));
        }

        #endregion

        #region Integration Tests

        [Fact]
        public async Task CompleteLifecycle_CreateUpdateDelete_ShouldWorkCorrectly()
        {
            // Arrange
            var addressSpaceId = TestConstants.DefaultAddressSpaceId;
            var ipId = "ip-lifecycle-test";

            var originalEntity = TestDataBuilders.CreateTestIpAllocationEntity(
                addressSpaceId,
                TestConstants.Networks.ChildNetwork1,
                ipId,
                new Dictionary<string, string> { { "Environment", "Test" } }
            );

            // Act 1: Create
            var createdEntity = await _ipAllocationRepository.CreateAsync(originalEntity);
            Assert.NotNull(createdEntity);
            Assert.Equal(ipId, createdEntity.Id);

            // Act 2: Read
            var retrievedEntity = await _ipAllocationRepository.GetByIdAsync(addressSpaceId, ipId);
            Assert.NotNull(retrievedEntity);
            Assert.Equal("Test", retrievedEntity.Tags["Environment"]);

            // Act 3: Update
            retrievedEntity.Tags["Environment"] = "Production";
            var updatedEntity = await _ipAllocationRepository.UpdateAsync(retrievedEntity);
            Assert.Equal("Production", updatedEntity.Tags["Environment"]);

            // Act 4: Delete
            await _ipAllocationRepository.DeleteAsync(addressSpaceId, ipId);
            var deletedEntity = await _ipAllocationRepository.GetByIdAsync(addressSpaceId, ipId);
            Assert.Null(deletedEntity);
        }

        #endregion

        #region Helper Methods

        private static bool IsValidCidr(string cidr)
        {
            try
            {
                var parts = cidr.Split('/');
                if (parts.Length != 2) return false;

                var address = System.Net.IPAddress.Parse(parts[0]);
                var prefixLength = int.Parse(parts[1]);

                if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    return prefixLength >= 0 && prefixLength <= 32;
                else
                    return prefixLength >= 0 && prefixLength <= 128;
            }
            catch
            {
                return false;
            }
        }

        #endregion

        public void Dispose()
        {
        }
    }
}