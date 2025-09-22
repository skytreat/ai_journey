using Xunit;
using Microsoft.Extensions.Configuration;
using Ipam.DataAccess.Entities;
using Ipam.DataAccess.Repositories;
using System.Threading.Tasks;

namespace Ipam.DataAccess.Tests.IntegrationTests
{
    public class RepositoryIntegrationTests
    {
        private readonly IConfiguration _configuration;
        private readonly UnitOfWork _unitOfWork;

        public RepositoryIntegrationTests()
        {
            _configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();

            var addressSpaceRepo = new AddressSpaceRepository(_configuration);
            var ipNodeRepo = new IpAllocationRepository(_configuration);
            var tagRepo = new TagRepository(_configuration);

            _unitOfWork = new UnitOfWork(addressSpaceRepo, ipNodeRepo, tagRepo);
        }

        [Fact]
        public async Task CompleteScenario_ShouldSucceed()
        {
            // Create address space
            var addressSpace = new AddressSpaceEntity
            {
                PartitionKey = "test",
                RowKey = "space1",
                Name = "TestSpace"
            };
            await _unitOfWork.AddressSpaces.CreateAsync(addressSpace);

            // Create tag
            var tag = new TagEntity
            {
                PartitionKey = "test",
                RowKey = "Environment",
                Type = "Inheritable"
            };
            await _unitOfWork.Tags.CreateAsync(tag);

            // Create IP node
            var ipNode = new IpAllocationEntity
            {
                PartitionKey = "test",
                RowKey = "ip1",
                Prefix = "10.0.0.0/8"
            };
            await _unitOfWork.IpNodes.CreateAsync(ipNode);

            // Verify everything was created
            var savedAddressSpace = await _unitOfWork.AddressSpaces.GetByIdAsync("test", "space1");
            Assert.NotNull(savedAddressSpace);

            var savedTag = await _unitOfWork.Tags.GetByNameAsync("test", "Environment");
            Assert.NotNull(savedTag);

            var savedIpNode = await _unitOfWork.IpNodes.GetByIdAsync("test", "ip1");
            Assert.NotNull(savedIpNode);
        }
    }
}
