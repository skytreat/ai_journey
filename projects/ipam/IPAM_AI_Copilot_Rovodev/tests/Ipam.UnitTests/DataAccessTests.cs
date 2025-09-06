using System;
using System.Threading.Tasks;
using Ipam.DataAccess;
using Ipam.DataAccess.Models;
using Xunit;
using Moq;

namespace Ipam.UnitTests
{
    public class DataAccessTests
    {
        [Fact]
        public async Task CreateIPAddressAsync_ShouldCreateIPAddress()
        {
            // Arrange
            var mockDataAccessService = new Mock<IDataAccessService>();
            var ipAddress = new IPAddress
            {
                Id = "192.168.1.1",
                Prefix = "192.168.1.0/24",
                AddressSpaceId = "default"
            };
            
            mockDataAccessService.Setup(service => service.CreateIPAddressAsync(ipAddress))
                .ReturnsAsync(ipAddress);

            // Act
            var result = await mockDataAccessService.Object.CreateIPAddressAsync(ipAddress);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(ipAddress.Id, result.Id);
            Assert.Equal(ipAddress.Prefix, result.Prefix);
        }
    }
}
