
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Ipam.Core;

namespace Ipam.DataAccess.Interfaces
{
    public interface ITagRepository
    {
        Task<Tag> GetTagAsync(Guid addressSpaceId, string name);
        Task<IEnumerable<Tag>> GetTagsAsync(Guid addressSpaceId);
        Task<Tag> CreateTagAsync(Tag tag);
        Task<Tag> UpdateTagAsync(Tag tag);
        Task DeleteTagAsync(Guid addressSpaceId, string name);

        // Merged from ITagImplicationRepository
        Task<TagImplication> GetTagImplicationAsync(Guid addressSpaceId, string ifTagValue);
        Task<IEnumerable<TagImplication>> GetTagImplicationsAsync(Guid addressSpaceId);
        Task<TagImplication> CreateTagImplicationAsync(TagImplication tagImplication);
        Task DeleteTagImplicationAsync(Guid addressSpaceId, string ifTagValue);
    }
}
