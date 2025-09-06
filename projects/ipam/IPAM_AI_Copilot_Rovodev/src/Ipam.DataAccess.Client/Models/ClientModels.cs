namespace Ipam.DataAccess.Client.Models
{
    /// <summary>
    /// Client-side DTOs that mirror the API DTOs
    /// </summary>
    /// <remarks>
    /// Author: IPAM Team
    /// Date: 2024-01-20
    /// </remarks>

    // Address Space DTOs
    public class AddressSpaceDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedOn { get; set; }
        public DateTime ModifiedOn { get; set; }
    }

    public class CreateAddressSpaceDto
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public class UpdateAddressSpaceDto
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    // IP Address DTOs
    public class IPAddressDto
    {
        public string Id { get; set; } = string.Empty;
        public string AddressSpaceId { get; set; } = string.Empty;
        public string Prefix { get; set; } = string.Empty;
        public string? ParentId { get; set; }
        public List<IPAddressTagDto> Tags { get; set; } = new();
        public DateTime CreatedOn { get; set; }
        public DateTime ModifiedOn { get; set; }
    }

    public class IPAddressTagDto
    {
        public string Name { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }

    public class CreateIPAddressDto
    {
        public string Prefix { get; set; } = string.Empty;
        public string? ParentId { get; set; }
        public List<CreateIPAddressTagDto> Tags { get; set; } = new();
    }

    public class CreateIPAddressTagDto
    {
        public string Name { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }

    public class UpdateIPAddressDto
    {
        public string Prefix { get; set; } = string.Empty;
        public string? ParentId { get; set; }
        public List<UpdateIPAddressTagDto> Tags { get; set; } = new();
    }

    public class UpdateIPAddressTagDto
    {
        public string Name { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }

    // Tag DTOs
    public class TagDto
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public TagType Type { get; set; }
        public List<string> KnownValues { get; set; } = new();
        public Dictionary<string, Dictionary<string, string>> Attributes { get; set; } = new();
        public Dictionary<string, Dictionary<string, string>> Implies { get; set; } = new();
        public DateTime CreatedOn { get; set; }
        public DateTime ModifiedOn { get; set; }
    }

    public class CreateTagDto
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public TagType Type { get; set; }
        public List<string> KnownValues { get; set; } = new();
        public Dictionary<string, Dictionary<string, string>> Attributes { get; set; } = new();
        public Dictionary<string, Dictionary<string, string>> Implies { get; set; } = new();
    }

    public class UpdateTagDto
    {
        public string Description { get; set; } = string.Empty;
        public TagType Type { get; set; }
        public List<string> KnownValues { get; set; } = new();
        public Dictionary<string, Dictionary<string, string>> Attributes { get; set; } = new();
        public Dictionary<string, Dictionary<string, string>> Implies { get; set; } = new();
    }

    public enum TagType
    {
        Inheritable = 0,
        NonInheritable = 1
    }
}