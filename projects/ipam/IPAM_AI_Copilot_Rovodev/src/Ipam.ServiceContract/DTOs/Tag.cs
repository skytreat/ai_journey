namespace Ipam.ServiceContract.DTOs;

public class Tag
{
    public string Name { get; set; } = string.Empty;
    public string AddressSpaceId { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Type { get; set; } = string.Empty;
    public DateTime CreatedOn { get; set; }
    public DateTime ModifiedOn { get; set; }
    public List<string> KnownValues { get; set; } = new List<string>();
    public Dictionary<string, Dictionary<string, string>> Implies { get; set; } = new Dictionary<string, Dictionary<string, string>>();
    public Dictionary<string, Dictionary<string, string>> Attributes { get; set; } = new Dictionary<string, Dictionary<string, string>>();
}