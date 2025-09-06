using IPAM.Application;
using IPAM.Domain;

namespace IPAM.Infrastructure;

public sealed class InMemoryAddressSpaceRepository : IAddressSpaceRepository
{
	private readonly Dictionary<Guid, AddressSpace> _store = new();
	public Task CreateAsync(AddressSpace space, CancellationToken ct)
	{
		_store[space.Id] = space;
		return Task.CompletedTask;
	}
	public Task DeleteAsync(Guid id, CancellationToken ct)
	{
		_store.Remove(id);
		return Task.CompletedTask;
	}
	public Task<AddressSpace?> GetAsync(Guid id, CancellationToken ct)
		=> Task.FromResult(_store.TryGetValue(id, out var v) ? v : null);
	public Task<IReadOnlyList<AddressSpace>> QueryAsync(string? nameKeyword, DateTimeOffset? createdAfter, DateTimeOffset? createdBefore, CancellationToken ct)
	{
		IEnumerable<AddressSpace> q = _store.Values;
		if (!string.IsNullOrWhiteSpace(nameKeyword)) q = q.Where(s => s.Name.Contains(nameKeyword, StringComparison.OrdinalIgnoreCase));
		if (createdAfter is not null) q = q.Where(s => s.CreatedOn >= createdAfter);
		if (createdBefore is not null) q = q.Where(s => s.CreatedOn <= createdBefore);
		return Task.FromResult<IReadOnlyList<AddressSpace>>(q.ToList());
	}
	public Task UpdateAsync(AddressSpace space, CancellationToken ct)
	{
		_store[space.Id] = space;
		return Task.CompletedTask;
	}
}

public sealed class InMemoryTagRepository : ITagRepository
{
	private readonly Dictionary<(Guid,string), TagDefinition> _store = new();
	public Task DeleteAsync(Guid addressSpaceId, string name, CancellationToken ct)
	{
		_store.Remove((addressSpaceId, name));
		return Task.CompletedTask;
	}
	public Task<TagDefinition?> GetAsync(Guid addressSpaceId, string name, CancellationToken ct)
		=> Task.FromResult(_store.TryGetValue((addressSpaceId, name), out var v) ? v : null);
	public Task<IReadOnlyList<TagDefinition>> QueryAsync(Guid addressSpaceId, string? nameKeyword, CancellationToken ct)
	{
		var q = _store.Values.Where(t => t.AddressSpaceId == addressSpaceId);
		if (!string.IsNullOrWhiteSpace(nameKeyword)) q = q.Where(t => t.Name.Contains(nameKeyword, StringComparison.OrdinalIgnoreCase));
		return Task.FromResult<IReadOnlyList<TagDefinition>>(q.ToList());
	}
	public Task UpsertAsync(TagDefinition tag, CancellationToken ct)
	{
		_store[(tag.AddressSpaceId, tag.Name)] = tag;
		return Task.CompletedTask;
	}
}

public sealed class InMemoryIpRepository : IIpRepository
{
	private readonly Dictionary<(Guid,Guid), IpCidr> _byId = new();
	private readonly Dictionary<(Guid,string), Guid> _byCidr = new();
	public Task DeleteAsync(Guid addressSpaceId, Guid id, CancellationToken ct)
	{
		if (_byId.Remove((addressSpaceId,id), out var ip))
		{
			_byCidr.Remove((addressSpaceId, ip.Prefix));
		}
		return Task.CompletedTask;
	}
	public Task<IReadOnlyList<IpCidr>> GetChildrenAsync(Guid addressSpaceId, Guid id, CancellationToken ct)
		=> Task.FromResult<IReadOnlyList<IpCidr>>(_byId.Values.Where(x => x.AddressSpaceId == addressSpaceId && x.ParentId == id).ToList());
	public Task<IpCidr?> GetByCidrAsync(Guid addressSpaceId, string cidr, CancellationToken ct)
	{
		if (_byCidr.TryGetValue((addressSpaceId, cidr), out var id) && _byId.TryGetValue((addressSpaceId, id), out var ip))
			return Task.FromResult<IpCidr?>(ip);
		return Task.FromResult<IpCidr?>(null);
	}
	public Task<IpCidr?> GetByIdAsync(Guid addressSpaceId, Guid id, CancellationToken ct)
		=> Task.FromResult(_byId.TryGetValue((addressSpaceId,id), out var v) ? v : null);
	public Task<IReadOnlyList<IpCidr>> QueryByTagsAsync(Guid addressSpaceId, IReadOnlyDictionary<string, string> tags, CancellationToken ct)
	{
		var q = _byId.Values.Where(x => x.AddressSpaceId == addressSpaceId);
		foreach (var kv in tags)
			q = q.Where(x => x.Tags.Any(t => t.Name.Equals(kv.Key, StringComparison.OrdinalIgnoreCase) && t.Value.Equals(kv.Value, StringComparison.OrdinalIgnoreCase)));
		return Task.FromResult<IReadOnlyList<IpCidr>>(q.ToList());
	}
	public Task UpsertAsync(IpCidr ip, CancellationToken ct)
	{
		_byId[(ip.AddressSpaceId, ip.Id)] = ip;
		_byCidr[(ip.AddressSpaceId, ip.Prefix)] = ip.Id;
		return Task.CompletedTask;
	}
}

public sealed class BasicCidrService : ICidrService
{
	public bool IsEqual(string a, string b) => string.Equals(Normalize(a), Normalize(b), StringComparison.OrdinalIgnoreCase);
	public bool IsParent(string parentCidr, string childCidr)
	{
		var p = Normalize(parentCidr);
		var c = Normalize(childCidr);
		if (p == c) return false;
		return c.StartsWith(p.Split('/')[0], StringComparison.OrdinalIgnoreCase) && int.Parse(c.Split('/')[1]) >= int.Parse(p.Split('/')[1]);
	}
	public bool IsValidCidr(string cidr)
	{
		if (string.IsNullOrWhiteSpace(cidr)) return false;
		cidr = cidr.Trim();
		return cidr.Contains('/') && System.Net.IPNetwork.TryParse(cidr, out _);
	}
	private static string Normalize(string cidr) => cidr.Trim();
}

public sealed class TagPolicyService : ITagPolicyService
{
	public void ValidateAssignment(TagDefinition definition, string value)
	{
		if (definition.KnownValues is { Count: > 0 } && !definition.KnownValues.Contains(value))
			throw new ArgumentException($"Value '{value}' not in KnownValues for tag {definition.Name}");
	}
	public IReadOnlyList<TagAssignment> ApplyImplications(TagDefinition definition, TagAssignment input, IReadOnlyCollection<TagAssignment> existingAssignments)
	{
		var result = new List<TagAssignment>();
		if (definition.Implications.TryGetValue(input.Value, out var implies))
		{
			foreach (var (tagName, impliedValue) in implies)
			{
				if (!existingAssignments.Any(t => t.Name.Equals(tagName, StringComparison.OrdinalIgnoreCase)))
					result.Add(new TagAssignment { Name = tagName, Value = impliedValue, IsInherited = false });
			}
		}
		return result;
	}
	public void ValidateInheritanceConsistency(IReadOnlyCollection<TagAssignment> parentInherited, IReadOnlyCollection<TagAssignment> childAssignments)
	{
		foreach (var p in parentInherited)
		{
			var conflict = childAssignments.FirstOrDefault(c => c.Name.Equals(p.Name, StringComparison.OrdinalIgnoreCase) && !c.Value.Equals(p.Value, StringComparison.OrdinalIgnoreCase));
			if (conflict is not null) throw new InvalidOperationException($"Inheritable tag conflict on {p.Name}");
		}
	}
	public bool ChildHasAdditionalInheritableTags(IReadOnlyCollection<TagAssignment> parentInherited, IReadOnlyCollection<TagAssignment> childAssignments)
	{
		var parentSet = parentInherited.Where(t => t.IsInherited).Select(t => (t.Name, t.Value)).ToHashSet();
		var childSet = childAssignments.Where(t => t.IsInherited).Select(t => (t.Name, t.Value)).ToHashSet();
		return childSet.IsSupersetOf(parentSet) && childSet.Count > parentSet.Count;
	}
}
