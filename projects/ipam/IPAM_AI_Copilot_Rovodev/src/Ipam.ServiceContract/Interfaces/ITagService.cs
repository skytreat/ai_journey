using Ipam.ServiceContract.DTOs;

namespace Ipam.ServiceContract.Interfaces;

public interface ITagService
{
    Task<Tag> CreateTagAsync(Tag tag, CancellationToken cancellationToken = default);
    Task<Tag?> GetTagAsync(string name, string addressSpaceId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Tag>> GetTagsAsync(string addressSpaceId, CancellationToken cancellationToken = default);
    Task<Tag> UpdateTagAsync(Tag tag, CancellationToken cancellationToken = default);
    Task DeleteTagAsync(string name, string addressSpaceId, CancellationToken cancellationToken = default);
}