using Azure;
using Azure.Data.Tables;
using System;
using System.Threading.Tasks;
using Ipam.DataAccess.Exceptions;

namespace Ipam.DataAccess.Extensions
{
    /// <summary>
    /// Extension methods for table entity operations
    /// </summary>
    public static class TableEntityExtensions
    {
        public static async Task<T> ExecuteWithRetryAsync<T>(
            this TableClient tableClient,
            Func<Task<T>> operation)
        {
            try
            {
                return await operation();
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                throw new EntityNotFoundException("Entity not found", ex);
            }
            catch (RequestFailedException ex) when (ex.Status == 412)
            {
                throw new ConcurrencyException("Entity was modified by another process", ex);
            }
            catch (Exception ex)
            {
                throw new IpamDataException("Operation failed", ex);
            }
        }
    }
}
