using Azure;
using Azure.Data.Tables;
using IPAM.Domain;

namespace IPAM.DataAccess;

internal static class TableNames
{
	public const string AddressSpaces = "AddressSpaces";
	public const string Tags = "Tags";
	public const string IpCidrs = "IpCidrs";
}

internal sealed class AddressSpaceEntity : ITableEntity
{
	public string PartitionKey { get; set; } = string.Empty;
	public string RowKey { get; set; } = string.Empty;
	public DateTimeOffset? Timestamp { get; set; }
	public ETag ETag { get; set; }

	public string Name { get; set; } = string.Empty;
	public string? Description { get; set; }
	public DateTimeOffset CreatedOn { get; set; }
	public DateTimeOffset ModifiedOn { get; set; }

	public static (string pk, string rk) Keys(Guid id) => ($"AS:{id}", $"AS:{id}");
	public static AddressSpaceEntity From(AddressSpace s)
	{
		var (pk, rk) = Keys(s.Id);
		return new AddressSpaceEntity
		{
			PartitionKey = pk,
			RowKey = rk,
			Name = s.Name,
			Description = s.Description,
			CreatedOn = s.CreatedOn,
			ModifiedOn = s.ModifiedOn
		};
	}
	public AddressSpace ToModel(Guid id) => new()
	{
		Id = id,
		Name = Name,
		Description = Description,
		CreatedOn = CreatedOn,
		ModifiedOn = ModifiedOn
	};
}

internal sealed class TagDefinitionEntity : ITableEntity
{
	public string PartitionKey { get; set; } = string.Empty;
	public string RowKey { get; set; } = string.Empty;
	public DateTimeOffset? Timestamp { get; set; }
	public ETag ETag { get; set; }

	public string Name { get; set; } = string.Empty;
	public string? Description { get; set; }
	public string Type { get; set; } = string.Empty;
	public string? KnownValuesJson { get; set; }
	public string AttributesJson { get; set; } = "{}";
	public string ImplicationsJson { get; set; } = "{}";
	public DateTimeOffset CreatedOn { get; set; }
	public DateTimeOffset ModifiedOn { get; set; }

	public static (string pk, string rk) Keys(Guid asId, string name) => ($"AS:{asId}", $"TAG:{name}");
	public static TagDefinitionEntity From(TagDefinition t)
	{
		var (pk, rk) = Keys(t.AddressSpaceId, t.Name);
		return new TagDefinitionEntity
		{
			PartitionKey = pk,
			RowKey = rk,
			Name = t.Name,
			Description = t.Description,
			Type = t.Type.ToString(),
			KnownValuesJson = t.KnownValues is null ? null : System.Text.Json.JsonSerializer.Serialize(t.KnownValues),
			AttributesJson = System.Text.Json.JsonSerializer.Serialize(t.Attributes),
			ImplicationsJson = System.Text.Json.JsonSerializer.Serialize(t.Implications),
			CreatedOn = t.CreatedOn,
			ModifiedOn = t.ModifiedOn
		};
	}
	public TagDefinition ToModel(Guid asId) => new()
	{
		AddressSpaceId = asId,
		Name = Name,
		Description = Description,
		Type = Enum.TryParse<TagType>(Type, out var tp) ? tp : TagType.NonInheritable,
		KnownValues = string.IsNullOrWhiteSpace(KnownValuesJson) ? null : System.Text.Json.JsonSerializer.Deserialize<List<string>>(KnownValuesJson!)!,
		Attributes = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, Dictionary<string,string>>>(AttributesJson) ?? new(),
		Implications = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, List<(string TagName, string Value)>>>(ImplicationsJson) ?? new(),
		CreatedOn = CreatedOn,
		ModifiedOn = ModifiedOn
	};
}

internal sealed class IpCidrEntity : ITableEntity
{
	public string PartitionKey { get; set; } = string.Empty;
	public string RowKey { get; set; } = string.Empty;
	public DateTimeOffset? Timestamp { get; set; }
	public ETag ETag { get; set; }

	public string Prefix { get; set; } = string.Empty;
	public string TagsJson { get; set; } = "[]";
	public DateTimeOffset CreatedOn { get; set; }
	public DateTimeOffset ModifiedOn { get; set; }
	public string? ParentId { get; set; }
	public string ChildrenIdsJson { get; set; } = "[]";

	public static (string pk, string rk) Keys(Guid asId, Guid id) => ($"AS:{asId}", $"IP:{id}");
	public static IpCidrEntity From(IpCidr ip)
	{
		var (pk, rk) = Keys(ip.AddressSpaceId, ip.Id);
		return new IpCidrEntity
		{
			PartitionKey = pk,
			RowKey = rk,
			Prefix = ip.Prefix,
			TagsJson = System.Text.Json.JsonSerializer.Serialize(ip.Tags),
			CreatedOn = ip.CreatedOn,
			ModifiedOn = ip.ModifiedOn,
			ParentId = ip.ParentId?.ToString(),
			ChildrenIdsJson = System.Text.Json.JsonSerializer.Serialize(ip.ChildrenIds)
		};
	}
	public IpCidr ToModel(Guid asId, Guid id) => new()
	{
		AddressSpaceId = asId,
		Id = id,
		Prefix = Prefix,
		Tags = System.Text.Json.JsonSerializer.Deserialize<List<TagAssignment>>(TagsJson) ?? new(),
		CreatedOn = CreatedOn,
		ModifiedOn = ModifiedOn,
		ParentId = Guid.TryParse(ParentId, out var p) ? p : null,
		ChildrenIds = System.Text.Json.JsonSerializer.Deserialize<List<Guid>>(ChildrenIdsJson) ?? new()
	};
}
