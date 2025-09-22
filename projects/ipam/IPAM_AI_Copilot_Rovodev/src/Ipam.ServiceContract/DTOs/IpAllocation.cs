namespace Ipam.ServiceContract.DTOs;

public class IpAllocation
{
    public string Id { get; set; } = string.Empty;
    public string AddressSpaceId { get; set; } = string.Empty;
    public string Prefix { get; set; } = string.Empty;
    public string? ParentId { get; set; }
    public List<string> ChildrenIds { get; set; } = new List<string>();
    public Dictionary<string, string> Tags { get; set; } = new Dictionary<string, string>();
    public DateTime CreatedOn { get; set; }
    public DateTime ModifiedOn { get; set; }
    public string Status { get; set; } = string.Empty;
}