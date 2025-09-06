using FluentAssertions;
using IPAM.Domain;

namespace Domain.Tests;

public class AddressSpaceTests
{
    [Fact]
    public void AddressSpace_ShouldInitializeWithDefaults()
    {
        // Arrange & Act
        var addressSpace = new AddressSpace();

        // Assert
        addressSpace.Id.Should().Be(Guid.Empty);
        addressSpace.Name.Should().BeEmpty();
        addressSpace.Description.Should().BeNull();
        addressSpace.CreatedOn.Should().Be(default);
        addressSpace.ModifiedOn.Should().Be(default);
    }

    [Fact]
    public void AddressSpace_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var id = Guid.NewGuid();
        var name = "Test Address Space";
        var description = "Test Description";
        var createdOn = DateTimeOffset.UtcNow;
        var modifiedOn = DateTimeOffset.UtcNow.AddMinutes(5);

        // Act
        var addressSpace = new AddressSpace
        {
            Id = id,
            Name = name,
            Description = description,
            CreatedOn = createdOn,
            ModifiedOn = modifiedOn
        };

        // Assert
        addressSpace.Id.Should().Be(id);
        addressSpace.Name.Should().Be(name);
        addressSpace.Description.Should().Be(description);
        addressSpace.CreatedOn.Should().Be(createdOn);
        addressSpace.ModifiedOn.Should().Be(modifiedOn);
    }
}

public class TagDefinitionTests
{
    [Fact]
    public void TagDefinition_ShouldInitializeWithDefaults()
    {
        // Arrange & Act
        var tag = new TagDefinition();

        // Assert
        tag.AddressSpaceId.Should().Be(Guid.Empty);
        tag.Name.Should().BeEmpty();
        tag.Description.Should().BeNull();
        tag.Type.Should().Be(default(TagType));
        tag.CreatedOn.Should().Be(default);
        tag.ModifiedOn.Should().Be(default);
    }

    [Fact]
    public void TagDefinition_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var addressSpaceId = Guid.NewGuid();
        var name = "Environment";
        var description = "Environment tag";
        var type = TagType.Inheritable;
        var createdOn = DateTimeOffset.UtcNow;
        var modifiedOn = DateTimeOffset.UtcNow.AddMinutes(5);

        // Act
        var tag = new TagDefinition
        {
            AddressSpaceId = addressSpaceId,
            Name = name,
            Description = description,
            Type = type,
            CreatedOn = createdOn,
            ModifiedOn = modifiedOn
        };

        // Assert
        tag.AddressSpaceId.Should().Be(addressSpaceId);
        tag.Name.Should().Be(name);
        tag.Description.Should().Be(description);
        tag.Type.Should().Be(type);
        tag.CreatedOn.Should().Be(createdOn);
        tag.ModifiedOn.Should().Be(modifiedOn);
    }

    [Theory]
    [InlineData(TagType.Inheritable)]
    [InlineData(TagType.NonInheritable)]
    public void TagDefinition_ShouldSupportAllTagTypes(TagType tagType)
    {
        // Arrange & Act
        var tag = new TagDefinition { Type = tagType };

        // Assert
        tag.Type.Should().Be(tagType);
    }
}

public class IpCidrTests
{
    [Fact]
    public void IpCidr_ShouldInitializeWithDefaults()
    {
        // Arrange & Act
        var ip = new IpCidr();

        // Assert
        ip.Id.Should().Be(Guid.Empty);
        ip.AddressSpaceId.Should().Be(Guid.Empty);
        ip.Prefix.Should().BeEmpty();
        ip.ParentId.Should().BeNull();
        ip.CreatedOn.Should().Be(default);
        ip.ModifiedOn.Should().Be(default);
    }

    [Fact]
    public void IpCidr_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var id = Guid.NewGuid();
        var addressSpaceId = Guid.NewGuid();
        var prefix = "192.168.1.0/24";
        var parentId = Guid.NewGuid();
        var createdOn = DateTimeOffset.UtcNow;
        var modifiedOn = DateTimeOffset.UtcNow.AddMinutes(5);

        // Act
        var ip = new IpCidr
        {
            Id = id,
            AddressSpaceId = addressSpaceId,
            Prefix = prefix,
            ParentId = parentId,
            CreatedOn = createdOn,
            ModifiedOn = modifiedOn
        };

        // Assert
        ip.Id.Should().Be(id);
        ip.AddressSpaceId.Should().Be(addressSpaceId);
        ip.Prefix.Should().Be(prefix);
        ip.ParentId.Should().Be(parentId);
        ip.CreatedOn.Should().Be(createdOn);
        ip.ModifiedOn.Should().Be(modifiedOn);
    }

    [Theory]
    [InlineData("192.168.1.0/24")]
    [InlineData("10.0.0.0/8")]
    [InlineData("172.16.0.0/16")]
    [InlineData("2001:db8::/32")]
    [InlineData("::1/128")]
    public void IpCidr_ShouldAcceptValidCidrFormats(string prefix)
    {
        // Arrange & Act
        var ip = new IpCidr { Prefix = prefix };

        // Assert
        ip.Prefix.Should().Be(prefix);
    }
}
