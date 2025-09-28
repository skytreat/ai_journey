using Ipam.DataAccess.Entities;
using Ipam.ServiceContract.DTOs;
using System;
using System.Collections.Generic;

namespace Ipam.DataAccess.Tests.TestHelpers
{
    /// <summary>
    /// Factory methods for creating test data objects with sensible defaults
    /// </summary>
    public static class TestDataBuilders
    {
        /// <summary>
        /// Creates a test IpAllocationEntity with default values
        /// </summary>
        public static IpAllocationEntity CreateTestIpAllocationEntity(
            string addressSpaceId = TestConstants.DefaultAddressSpaceId,
            string prefix = TestConstants.DefaultCidr,
            string? id = null,
            Dictionary<string, string>? tags = null)
        {
            return new IpAllocationEntity
            {
                Id = id ?? Guid.NewGuid().ToString(),
                AddressSpaceId = addressSpaceId,
                Prefix = prefix,
                Tags = tags ?? TestConstants.Tags.DefaultTags,
                CreatedOn = DateTime.UtcNow,
                ModifiedOn = DateTime.UtcNow,
                ChildrenIds = new List<string>()
            };
        }
        
        /// <summary>
        /// Creates a test TagEntity with default values
        /// </summary>
        public static TagEntity CreateTestTagEntity(
            string addressSpaceId = TestConstants.DefaultAddressSpaceId,
            string name = TestConstants.Tags.EnvironmentTagName,
            string type = "Inheritable",
            List<string>? knownValues = null)
        {
            return new TagEntity
            {
                AddressSpaceId = addressSpaceId,
                Name = name,
                Type = type,
                Description = $"Test {name} tag",
                KnownValues = knownValues ?? new List<string> { "Production", "Development", "Testing" },
                CreatedOn = DateTime.UtcNow,
                ModifiedOn = DateTime.UtcNow,
                Version = 1,
                Implies = new Dictionary<string, Dictionary<string, string>>(),
                Attributes = new Dictionary<string, Dictionary<string, string>>()
            };
        }
        
        /// <summary>
        /// Creates a test AddressSpaceEntity with default values
        /// </summary>
        public static AddressSpaceEntity CreateTestAddressSpaceEntity(
            string? id = null,
            string name = "Test Address Space",
            string cidr = TestConstants.Networks.ParentNetwork)
        {
            return new AddressSpaceEntity
            {
                Id = id ?? TestConstants.DefaultAddressSpaceId,
                Name = name,
                Description = $"Test address space for {name}",
                CreatedOn = DateTime.UtcNow,
                ModifiedOn = DateTime.UtcNow
            };
        }
        
        /// <summary>
        /// Creates a test Tag DTO with default values
        /// </summary>
        public static Tag CreateTestTag(
            string name = TestConstants.Tags.EnvironmentTagName,
            string type = "Inheritable",
            List<string>? knownValues = null)
        {
            return new Tag
            {
                Name = name,
                Type = type,
                Description = $"Test {name} tag",
                KnownValues = knownValues ?? new List<string> { "Production", "Development", "Testing" },
                CreatedOn = DateTime.UtcNow,
                ModifiedOn = DateTime.UtcNow
            };
        }
        
        /// <summary>
        /// Creates a test IpAllocation DTO with default values
        /// </summary>
        public static IpAllocation CreateTestIpAllocation(
            string addressSpaceId = TestConstants.DefaultAddressSpaceId,
            string prefix = TestConstants.DefaultCidr,
            string? id = null,
            Dictionary<string, string>? tags = null)
        {
            return new IpAllocation
            {
                Id = id ?? Guid.NewGuid().ToString(),
                AddressSpaceId = addressSpaceId,
                Prefix = prefix,
                Tags = tags ?? TestConstants.Tags.DefaultTags,
                Status = "Allocated",
                CreatedOn = DateTime.UtcNow,
                ModifiedOn = DateTime.UtcNow,
                // Children property may not exist - remove if not available
            };
        }
        
        /// <summary>
        /// Creates a test AddressSpace DTO with default values
        /// </summary>
        public static AddressSpace CreateTestAddressSpace(
            string? id = null,
            string name = "Test Address Space",
            string cidr = TestConstants.Networks.ParentNetwork)
        {
            return new AddressSpace
            {
                Id = id ?? TestConstants.DefaultAddressSpaceId,
                Name = name,
                Description = $"Test address space for {name}",
                // Cidr property may not exist - remove if not available
                CreatedOn = DateTime.UtcNow,
                ModifiedOn = DateTime.UtcNow
            };
        }
        
        /// <summary>
        /// Creates multiple test entities for bulk operations
        /// </summary>
        public static List<IpAllocationEntity> CreateTestIpAllocationEntities(
            int count,
            string addressSpaceId = TestConstants.DefaultAddressSpaceId,
            string basePrefix = "10.0")
        {
            var entities = new List<IpAllocationEntity>();
            for (int i = 0; i < count; i++)
            {
                entities.Add(CreateTestIpAllocationEntity(
                    addressSpaceId,
                    $"{basePrefix}.{i}.0/24",
                    $"test-ip-{i}"));
            }
            return entities;
        }
        
        /// <summary>
        /// Creates a hierarchy of parent-child IP allocations for testing
        /// </summary>
        public static (IpAllocationEntity parent, List<IpAllocationEntity> children) CreateTestIpHierarchy(
            string addressSpaceId = TestConstants.DefaultAddressSpaceId)
        {
            var parent = CreateTestIpAllocationEntity(addressSpaceId, TestConstants.Networks.ParentNetwork, "parent-ip");
            
            var child1 = CreateTestIpAllocationEntity(addressSpaceId, TestConstants.Networks.ChildNetwork1, "child-ip-1");
            var child2 = CreateTestIpAllocationEntity(addressSpaceId, TestConstants.Networks.ChildNetwork2, "child-ip-2");
            
            parent.ChildrenIds = new List<string> { child1.Id, child2.Id };
            
            return (parent, new List<IpAllocationEntity> { child1, child2 });
        }
    }
}