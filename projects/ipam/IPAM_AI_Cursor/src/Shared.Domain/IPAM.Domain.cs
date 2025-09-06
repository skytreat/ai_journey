namespace IPAM.Domain;

public enum TagType
{
	Inheritable,
	NonInheritable
}

public sealed class AddressSpace
{
	public Guid Id { get; set; }
	public string Name { get; set; } = string.Empty;
	public string? Description { get; set; }
	public DateTimeOffset CreatedOn { get; set; }
	public DateTimeOffset ModifiedOn { get; set; }
}

public sealed class TagDefinition
{
	public Guid AddressSpaceId { get; set; }
	public string Name { get; set; } = string.Empty;
	public string? Description { get; set; }
	public TagType Type { get; set; }
	public List<string>? KnownValues { get; set; }
	public Dictionary<string, Dictionary<string, string>> Attributes { get; set; } = new();
	public Dictionary<string, List<(string TagName, string Value)>> Implications { get; set; } = new();
	public DateTimeOffset CreatedOn { get; set; }
	public DateTimeOffset ModifiedOn { get; set; }
}

public sealed class TagAssignment
{
	public string Name { get; set; } = string.Empty;
	public string Value { get; set; } = string.Empty;
	public bool IsInherited { get; set; }
}

public sealed class IpCidr
{
	public Guid AddressSpaceId { get; set; }
	public Guid Id { get; set; }
	public string Prefix { get; set; } = string.Empty;
	public List<TagAssignment> Tags { get; set; } = new();
	public DateTimeOffset CreatedOn { get; set; }
	public DateTimeOffset ModifiedOn { get; set; }
	public Guid? ParentId { get; set; }
	public List<Guid> ChildrenIds { get; set; } = new();
}
