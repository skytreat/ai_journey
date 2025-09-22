namespace Ipam.ServiceContract.DTOs;

public class AddressSpace
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedOn { get; set; }
    public DateTime ModifiedOn { get; set; }
    public string Status { get; set; } = string.Empty;
}