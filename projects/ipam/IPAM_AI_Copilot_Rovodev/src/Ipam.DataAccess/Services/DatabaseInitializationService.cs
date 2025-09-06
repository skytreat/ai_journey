using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System.Threading;
using System.Threading.Tasks;
using Ipam.DataAccess.Models;
using Ipam.DataAccess.Configuration;
using Ipam.DataAccess.Interfaces;
using System.Collections.Generic;
using System;

namespace Ipam.DataAccess.Services
{
    /// <summary>
    /// Service for initializing database tables and default data
    /// </summary>
    /// <remarks>
    /// Author: IPAM Team
    /// Date: 2024-01-20
    /// </remarks>
    public class DatabaseInitializationService : IHostedService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly DataAccessOptions _options;

        public DatabaseInitializationService(
            IUnitOfWork unitOfWork,
            IOptions<DataAccessOptions> options)
        {
            _unitOfWork = unitOfWork;
            _options = options.Value;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            // Create default root address spaces and IP nodes
            await InitializeRootAddressSpace(cancellationToken);
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        private async Task InitializeRootAddressSpace(CancellationToken cancellationToken)
        {
            var rootIpv6 = new IpNode
            {
                PartitionKey = "system",
                RowKey = "ipv6_root",
                Prefix = "::/0",
                Tags = new Dictionary<string, string> { { "Type", "Root" } }
            };

            var rootIpv4 = new IpNode
            {
                PartitionKey = "system",
                RowKey = "ipv4_root",
                Prefix = "0.0.0.0/0",
                ParentId = "ipv6_root",
                Tags = new Dictionary<string, string> { { "Type", "Root" } }
            };

            try
            {
                await _unitOfWork.IpNodes.CreateAsync(rootIpv6);
                await _unitOfWork.IpNodes.CreateAsync(rootIpv4);
                await _unitOfWork.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // Log initialization error but don't block service startup
                // Root nodes might already exist
            }
        }
    }
}
