using Azure;
using Azure.Data.Tables;
using IPAM.Application;
using IPAM.Domain;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace IPAM.DataAccess;

public sealed class TableOptions
{
	public string ConnectionString { get; set; } = "UseDevelopmentStorage=true";
}

internal sealed class TableClientFactory
{
	private readonly TableServiceClient _service;
	public TableClientFactory(IOptions<TableOptions> options)
	{
		_service = new TableServiceClient(options.Value.ConnectionString);
	}
	public TableClient Get(string table)
	{
		var client = _service.GetTableClient(table);
		client.CreateIfNotExists();
		return client;
	}
}

public static class DataAccessRegistration
{
	public static IServiceCollection AddIpamTableStorage(this IServiceCollection services, Action<TableOptions>? configure = null)
	{
		if (configure is not null) services.Configure(configure);
		else services.Configure<TableOptions>(_ => { });
		services.AddSingleton<TableClientFactory>();
		services.AddScoped<IAddressSpaceRepository, AddressSpaceTableRepository>();
		services.AddScoped<ITagRepository, TagTableRepository>();
		services.AddScoped<IIpRepository, IpTableRepository>();
		return services;
	}
}

internal sealed class AddressSpaceTableRepository : IAddressSpaceRepository
{
	private readonly TableClient _table;
	public AddressSpaceTableRepository(TableClientFactory factory)
	{
		_table = factory.Get(TableNames.AddressSpaces);
	}
	public async Task CreateAsync(AddressSpace space, CancellationToken ct)
	{
		var e = AddressSpaceEntity.From(space);
		await _table.UpsertEntityAsync(e, TableUpdateMode.Replace, ct);
	}
	public async Task DeleteAsync(Guid id, CancellationToken ct)
	{
		var (pk, rk) = AddressSpaceEntity.Keys(id);
		await _table.DeleteEntityAsync(pk, rk, ETag.All, ct);
	}
	public async Task<AddressSpace?> GetAsync(Guid id, CancellationToken ct)
	{
		var (pk, rk) = AddressSpaceEntity.Keys(id);
		try
		{
			var res = await _table.GetEntityAsync<AddressSpaceEntity>(pk, rk, cancellationToken: ct);
			return res.Value.ToModel(id);
		}
		catch (RequestFailedException ex) when (ex.Status == 404)
		{
			return null;
		}
	}
	public async Task<IReadOnlyList<AddressSpace>> QueryAsync(string? nameKeyword, DateTimeOffset? createdAfter, DateTimeOffset? createdBefore, CancellationToken ct)
	{
		var list = new List<AddressSpace>();
		await foreach (var e in _table.QueryAsync<AddressSpaceEntity>(cancellationToken: ct))
		{
			var id = Guid.Parse(e.RowKey[3..]);
			var model = e.ToModel(id);
			if (!string.IsNullOrWhiteSpace(nameKeyword) && !model.Name.Contains(nameKeyword, StringComparison.OrdinalIgnoreCase)) continue;
			if (createdAfter is not null && model.CreatedOn < createdAfter) continue;
			if (createdBefore is not null && model.CreatedOn > createdBefore) continue;
			list.Add(model);
		}
		return list;
	}
	public Task UpdateAsync(AddressSpace space, CancellationToken ct) => CreateAsync(space, ct);
}

internal sealed class TagTableRepository : ITagRepository
{
	private readonly TableClient _table;
	public TagTableRepository(TableClientFactory factory) => _table = factory.Get(TableNames.Tags);
	public async Task DeleteAsync(Guid addressSpaceId, string name, CancellationToken ct)
	{
		var (pk, rk) = TagDefinitionEntity.Keys(addressSpaceId, name);
		await _table.DeleteEntityAsync(pk, rk, ETag.All, ct);
	}
	public async Task<TagDefinition?> GetAsync(Guid addressSpaceId, string name, CancellationToken ct)
	{
		var (pk, rk) = TagDefinitionEntity.Keys(addressSpaceId, name);
		try
		{
			var res = await _table.GetEntityAsync<TagDefinitionEntity>(pk, rk, cancellationToken: ct);
			return res.Value.ToModel(addressSpaceId);
		}
		catch (RequestFailedException ex) when (ex.Status == 404)
		{
			return null;
		}
	}
	public async Task<IReadOnlyList<TagDefinition>> QueryAsync(Guid addressSpaceId, string? nameKeyword, CancellationToken ct)
	{
		var list = new List<TagDefinition>();
		var pk = $"AS:{addressSpaceId}";
		await foreach (var e in _table.QueryAsync<TagDefinitionEntity>(x => x.PartitionKey == pk, cancellationToken: ct))
		{
			var model = e.ToModel(addressSpaceId);
			if (!string.IsNullOrWhiteSpace(nameKeyword) && !model.Name.Contains(nameKeyword, StringComparison.OrdinalIgnoreCase)) continue;
			list.Add(model);
		}
		return list;
	}
	public async Task UpsertAsync(TagDefinition tag, CancellationToken ct)
	{
		var e = TagDefinitionEntity.From(tag);
		await _table.UpsertEntityAsync(e, TableUpdateMode.Replace, ct);
	}
}

internal sealed class IpTableRepository : IIpRepository
{
	private readonly TableClient _table;
	public IpTableRepository(TableClientFactory factory) => _table = factory.Get(TableNames.IpCidrs);
	public async Task DeleteAsync(Guid addressSpaceId, Guid id, CancellationToken ct)
	{
		var (pk, rk) = IpCidrEntity.Keys(addressSpaceId, id);
		await _table.DeleteEntityAsync(pk, rk, ETag.All, ct);
	}
	public async Task<IReadOnlyList<IpCidr>> GetChildrenAsync(Guid addressSpaceId, Guid id, CancellationToken ct)
	{
		var list = new List<IpCidr>();
		var pk = $"AS:{addressSpaceId}";
		await foreach (var e in _table.QueryAsync<IpCidrEntity>(x => x.PartitionKey == pk && x.ParentId == id.ToString(), cancellationToken: ct))
		{
			var model = e.ToModel(addressSpaceId, Guid.Parse(e.RowKey[3..]));
			list.Add(model);
		}
		return list;
	}
	public async Task<IpCidr?> GetByCidrAsync(Guid addressSpaceId, string cidr, CancellationToken ct)
	{
		var pk = $"AS:{addressSpaceId}";
		await foreach (var e in _table.QueryAsync<IpCidrEntity>(x => x.PartitionKey == pk && x.Prefix == cidr, cancellationToken: ct))
		{
			return e.ToModel(addressSpaceId, Guid.Parse(e.RowKey[3..]));
		}
		return null;
	}
	public async Task<IpCidr?> GetByIdAsync(Guid addressSpaceId, Guid id, CancellationToken ct)
	{
		var (pk, rk) = IpCidrEntity.Keys(addressSpaceId, id);
		try
		{
			var res = await _table.GetEntityAsync<IpCidrEntity>(pk, rk, cancellationToken: ct);
			return res.Value.ToModel(addressSpaceId, id);
		}
		catch (RequestFailedException ex) when (ex.Status == 404)
		{
			return null;
		}
	}
	public async Task<IReadOnlyList<IpCidr>> QueryByTagsAsync(Guid addressSpaceId, IReadOnlyDictionary<string, string> tags, CancellationToken ct)
	{
		if (tags.Count == 0)
		{
			// return all in address space
			var listAll = new List<IpCidr>();
			var pk = $"AS:{addressSpaceId}";
			await foreach (var e in _table.QueryAsync<IpCidrEntity>(x => x.PartitionKey == pk, cancellationToken: ct))
				listAll.Add(e.ToModel(addressSpaceId, Guid.Parse(e.RowKey[3..])));
			return listAll;
		}
		var result = new List<IpCidr>();
		var pk2 = $"AS:{addressSpaceId}";
		await foreach (var e in _table.QueryAsync<IpCidrEntity>(x => x.PartitionKey == pk2, cancellationToken: ct))
		{
			var model = e.ToModel(addressSpaceId, Guid.Parse(e.RowKey[3..]));
			bool ok = true;
			foreach (var kv in tags)
			{
				if (!model.Tags.Any(t => t.Name.Equals(kv.Key, StringComparison.OrdinalIgnoreCase) && t.Value.Equals(kv.Value, StringComparison.OrdinalIgnoreCase)))
				{
					ok = false; break;
				}
			}
			if (ok) result.Add(model);
		}
		return result;
	}
	public async Task UpsertAsync(IpCidr ip, CancellationToken ct)
	{
		var e = IpCidrEntity.From(ip);
		await _table.UpsertEntityAsync(e, TableUpdateMode.Replace, ct);
	}
}
