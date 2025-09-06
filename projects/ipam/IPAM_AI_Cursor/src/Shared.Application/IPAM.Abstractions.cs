using IPAM.Domain;
using IPAM.Contracts;

namespace IPAM.Application;

public interface IAddressSpaceRepository
{
	Task<AddressSpace?> GetAsync(Guid id, CancellationToken ct);
	Task<IReadOnlyList<AddressSpace>> QueryAsync(string? nameKeyword, DateTimeOffset? createdAfter, DateTimeOffset? createdBefore, CancellationToken ct);
	Task CreateAsync(AddressSpace space, CancellationToken ct);
	Task UpdateAsync(AddressSpace space, CancellationToken ct);
	Task DeleteAsync(Guid id, CancellationToken ct);
}

public interface ITagRepository
{
	Task<TagDefinition?> GetAsync(Guid addressSpaceId, string name, CancellationToken ct);
	Task<IReadOnlyList<TagDefinition>> QueryAsync(Guid addressSpaceId, string? nameKeyword, CancellationToken ct);
	Task UpsertAsync(TagDefinition tag, CancellationToken ct);
	Task DeleteAsync(Guid addressSpaceId, string name, CancellationToken ct);
}

public interface IIpRepository
{
	Task<IpCidr?> GetByIdAsync(Guid addressSpaceId, Guid id, CancellationToken ct);
	Task<IpCidr?> GetByCidrAsync(Guid addressSpaceId, string cidr, CancellationToken ct);
	Task<IReadOnlyList<IpCidr>> QueryByTagsAsync(Guid addressSpaceId, IReadOnlyDictionary<string,string> tags, CancellationToken ct);
	Task<IReadOnlyList<IpCidr>> GetChildrenAsync(Guid addressSpaceId, Guid id, CancellationToken ct);
	Task UpsertAsync(IpCidr ip, CancellationToken ct);
	Task DeleteAsync(Guid addressSpaceId, Guid id, CancellationToken ct);
}

public interface ITagPolicyService
{
	void ValidateAssignment(TagDefinition definition, string value);
	IReadOnlyList<TagAssignment> ApplyImplications(TagDefinition definition, TagAssignment input, IReadOnlyCollection<TagAssignment> existingAssignments);
	void ValidateInheritanceConsistency(IReadOnlyCollection<TagAssignment> parentInherited, IReadOnlyCollection<TagAssignment> childAssignments);
	bool ChildHasAdditionalInheritableTags(IReadOnlyCollection<TagAssignment> parentInherited, IReadOnlyCollection<TagAssignment> childAssignments);
}

public interface ICidrService
{
	bool IsValidCidr(string cidr);
	bool IsParent(string parentCidr, string childCidr);
	bool IsEqual(string a, string b);
}
