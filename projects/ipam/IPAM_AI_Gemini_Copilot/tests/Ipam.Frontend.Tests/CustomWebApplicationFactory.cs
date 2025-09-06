using System;
using System.Collections.Generic;
using System.Linq;
using Azure;
using Azure.Data.Tables;
using Azure.Data.Tables.Models;
using Ipam.DataAccess.Entities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Ipam.Frontend.Tests
{
    public class CustomWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram>
        where TProgram : class
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                // Remove the existing TableServiceClient registration
                var tableServiceClientDescriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(TableServiceClient));

                if (tableServiceClientDescriptor != null)
                {
                    services.Remove(tableServiceClientDescriptor);
                }

                // Mock TableServiceClient and TableClient
                var mockTableServiceClient = new Mock<TableServiceClient>();
                var mockTableClient = new Mock<TableClient>();

                // Configure mockTableServiceClient to return mockTableClient for any table name
                mockTableServiceClient.Setup(x => x.GetTableClient(It.IsAny<string>()))
                    .Returns(mockTableClient.Object);

                // Mock AddEntityAsync to return a successful response
                mockTableClient.Setup(x => x.AddEntityAsync(It.IsAny<ITableEntity>(), It.IsAny<System.Threading.CancellationToken>()))
                    .Returns(Task.FromResult(new Mock<Response>().Object));

                // Mock GetEntityAsync to return a UserEntity for the test user
                mockTableClient.Setup(x => x.GetEntityAsync<UserEntity>("SYSTEM", It.IsAny<string>(), null, It.IsAny<System.Threading.CancellationToken>()))
                    .ReturnsAsync((string pk, string rk, System.Collections.Generic.IEnumerable<string> sel, System.Threading.CancellationToken ct) =>
                    {
                        // Return a mock UserEntity for the registered user
                        if (rk.StartsWith("testuser"))
                        {
                            return Response.FromValue(new UserEntity { PartitionKey = pk, RowKey = rk, PasswordHash = "Password123!" }, new Mock<Response>().Object);
                        }
                        // Throw an exception if the user is not found, mimicking Azure Table Storage behavior
                        throw new RequestFailedException(404, "Entity not found.");
                    });

                // Mock QueryAsync for RoleEntity
                mockTableClient.Setup(x => x.QueryAsync<RoleEntity>(It.IsAny<string>(), null, null, It.IsAny<System.Threading.CancellationToken>()))
                    .Returns((string filter, int? maxPerPage, System.Collections.Generic.IEnumerable<string> select, System.Threading.CancellationToken ct) =>
                    {
                        var rolesList = new List<RoleEntity>();
                        // Assuming "testuser" is the username used in tests that call GetRolesAsync
                        if (filter.Contains("PartitionKey eq 'testuser'"))
                        {
                            rolesList.Add(new RoleEntity { PartitionKey = "testuser", RowKey = Guid.NewGuid().ToString(), Role = "Admin" });
                        }
                        var mockAsyncPageable = new Mock<AsyncPageable<RoleEntity>>();
                        mockAsyncPageable.Setup(ap => ap.GetAsyncEnumerator(It.IsAny<System.Threading.CancellationToken>()))
                            .Returns(rolesList.ToAsyncEnumerable().GetAsyncEnumerator());
                        return mockAsyncPageable.Object;
                    });

                // Register the mock TableServiceClient
                services.AddSingleton(mockTableServiceClient.Object);
            });
        }
    }
}
