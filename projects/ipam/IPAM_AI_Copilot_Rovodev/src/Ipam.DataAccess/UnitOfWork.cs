using Ipam.DataAccess.Interfaces;
using System;
using System.Threading.Tasks;
using System.Transactions;

namespace Ipam.DataAccess
{
    /// <summary>
    /// Implementation of unit of work pattern for IPAM repositories
    /// </summary>
    /// <remarks>
    /// Author: IPAM Team
    /// Date: 2024-01-20
    /// </remarks>
    public class UnitOfWork : IUnitOfWork
    {
        public IAddressSpaceRepository AddressSpaces { get; }
        public IIpAllocationRepository IpNodes { get; }
        public ITagRepository Tags { get; }

        public UnitOfWork(
            IAddressSpaceRepository addressSpaces,
            IIpAllocationRepository ipNodes,
            ITagRepository tags)
        {
            AddressSpaces = addressSpaces;
            IpNodes = ipNodes;
            Tags = tags;
        }

        public async Task SaveChangesAsync()
        {
            using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                try
                {
                    // 实际项目中这里需要实现事务提交逻辑
                    await Task.CompletedTask;
                    scope.Complete();
                }
                catch (Exception)
                {
                    // 记录错误并重新抛出
                    throw;
                }
            }
        }
    }
}
