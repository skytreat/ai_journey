
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Ipam.Core;

namespace Ipam.DataAccess.Interfaces
{
    public interface ITagImplicationRepository
    {
        Task<TagImplication> GetTagImplicationAsync(Guid addressSpaceId, string ifTagValue);
        Task<IEnumerable<TagImplication>> GetTagImplicationsAsync(Guid addressSpaceId);
        Task<TagImplication> CreateTagImplicationAsync(TagImplication tagImplication);
        Task DeleteTagImplicationAsync(Guid addressSpaceId, string ifTagValue);
    }
}
