namespace IPAM.Contracts;

public sealed record PaginatedResult<T>(List<T> Items, int TotalCount, int PageNumber, int PageSize, int TotalPages);

public sealed record PaginationParameters(int PageNumber = 1, int PageSize = 20)
{
	public int PageNumber { get; init; } = Math.Max(1, PageNumber);
	public int PageSize { get; init; } = Math.Clamp(PageSize, 1, 100);
}

public record AddressSpaceDto
{
	public Guid Id { get; init; }
	public string Name { get; init; } = string.Empty;
	public string? Description { get; init; }
	public DateTimeOffset CreatedOn { get; init; }
	public DateTimeOffset ModifiedOn { get; init; }
}

public enum TagTypeDto
{
	Inheritable,
	NonInheritable
}

public record TagDefinitionDto
{
	public Guid AddressSpaceId { get; init; }
	public string Name { get; init; } = string.Empty;
	public string? Description { get; init; }
	public TagTypeDto Type { get; init; }
	public IReadOnlyList<string>? KnownValues { get; init; }
	public IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> Attributes { get; init; } = new Dictionary<string, IReadOnlyDictionary<string, string>>();
	public IReadOnlyDictionary<string, IReadOnlyList<(string TagName, string Value)>> Implications { get; init; } = new Dictionary<string, IReadOnlyList<(string TagName, string Value)>>();
	public DateTimeOffset CreatedOn { get; init; }
	public DateTimeOffset ModifiedOn { get; init; }
}

public record TagAssignmentDto
{
	public string Name { get; init; } = string.Empty;
	public string Value { get; init; } = string.Empty;
	public bool IsInherited { get; init; }
}

public record IpCidrDto
{
	public Guid AddressSpaceId { get; init; }
	public Guid Id { get; init; }
	public string Prefix { get; init; } = string.Empty;
	public IReadOnlyList<TagAssignmentDto> Tags { get; init; } = Array.Empty<TagAssignmentDto>();
	public DateTimeOffset CreatedOn { get; init; }
	public DateTimeOffset ModifiedOn { get; init; }
	public Guid? ParentId { get; init; }
	public IReadOnlyList<Guid> ChildrenIds { get; init; } = Array.Empty<Guid>();
}
