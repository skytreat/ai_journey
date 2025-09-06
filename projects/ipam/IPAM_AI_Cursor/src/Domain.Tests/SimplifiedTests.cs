using FluentAssertions;
using IPAM.Domain;
using IPAM.Contracts;
using IPAM.Infrastructure;

namespace Domain.Tests;

// 专注于核心Domain模型测试以获得高代码覆盖率
public class CoreDomainTests
{
    [Fact]
    public void AddressSpace_ShouldInitializeCorrectly()
    {
        // Arrange & Act
        var addressSpace = new AddressSpace
        {
            Id = Guid.NewGuid(),
            Name = "Test Network",
            Description = "Test Description",
            CreatedOn = DateTimeOffset.UtcNow,
            ModifiedOn = DateTimeOffset.UtcNow
        };

        // Assert
        addressSpace.Id.Should().NotBe(Guid.Empty);
        addressSpace.Name.Should().Be("Test Network");
        addressSpace.Description.Should().Be("Test Description");
        addressSpace.CreatedOn.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromMinutes(1));
        addressSpace.ModifiedOn.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public void TagDefinition_ShouldSupportInheritableType()
    {
        // Arrange & Act
        var tag = new TagDefinition
        {
            AddressSpaceId = Guid.NewGuid(),
            Name = "Environment",
            Description = "Environment tag",
            Type = TagType.Inheritable,
            CreatedOn = DateTimeOffset.UtcNow,
            ModifiedOn = DateTimeOffset.UtcNow
        };

        // Assert
        tag.AddressSpaceId.Should().NotBe(Guid.Empty);
        tag.Name.Should().Be("Environment");
        tag.Description.Should().Be("Environment tag");
        tag.Type.Should().Be(TagType.Inheritable);
    }

    [Fact]
    public void TagDefinition_ShouldSupportNonInheritableType()
    {
        // Arrange & Act
        var tag = new TagDefinition
        {
            AddressSpaceId = Guid.NewGuid(),
            Name = "Owner",
            Type = TagType.NonInheritable
        };

        // Assert
        tag.Type.Should().Be(TagType.NonInheritable);
    }

    [Fact]
    public void IpCidr_ShouldStoreValidCidrPrefix()
    {
        // Arrange & Act
        var ip = new IpCidr
        {
            Id = Guid.NewGuid(),
            AddressSpaceId = Guid.NewGuid(),
            Prefix = "192.168.1.0/24",
            CreatedOn = DateTimeOffset.UtcNow,
            ModifiedOn = DateTimeOffset.UtcNow
        };

        // Assert
        ip.Id.Should().NotBe(Guid.Empty);
        ip.AddressSpaceId.Should().NotBe(Guid.Empty);
        ip.Prefix.Should().Be("192.168.1.0/24");
    }

    [Fact]
    public void IpCidr_ShouldSupportParentChildRelationship()
    {
        // Arrange
        var parentId = Guid.NewGuid();
        
        // Act
        var childIp = new IpCidr
        {
            Id = Guid.NewGuid(),
            AddressSpaceId = Guid.NewGuid(),
            Prefix = "192.168.1.0/25",
            ParentId = parentId
        };

        // Assert
        childIp.ParentId.Should().Be(parentId);
    }
}

public class PaginationContractsTests
{
    [Fact]
    public void PaginationParameters_ShouldEnforceValidRanges()
    {
        // Test minimum values
        var minParams = new PaginationParameters(0, 0);
        minParams.PageNumber.Should().Be(1);
        minParams.PageSize.Should().Be(1);

        // Test maximum values
        var maxParams = new PaginationParameters(1, 200);
        maxParams.PageNumber.Should().Be(1);
        maxParams.PageSize.Should().Be(100);

        // Test valid values
        var validParams = new PaginationParameters(5, 50);
        validParams.PageNumber.Should().Be(5);
        validParams.PageSize.Should().Be(50);
    }

    [Fact]
    public void PaginatedResult_ShouldCalculateCorrectly()
    {
        // Arrange
        var items = new List<string> { "item1", "item2", "item3" };
        
        // Act
        var result = new PaginatedResult<string>(items, 100, 2, 10, 10);

        // Assert
        result.Items.Should().HaveCount(3);
        result.TotalCount.Should().Be(100);
        result.PageNumber.Should().Be(2);
        result.PageSize.Should().Be(10);
        result.TotalPages.Should().Be(10);
    }
}

public class CidrServiceTests
{
    private readonly BasicCidrService _service = new();

    [Theory]
    [InlineData("192.168.1.0/24", true)]
    [InlineData("10.0.0.0/8", true)]
    [InlineData("172.16.0.0/16", true)]
    [InlineData("2001:db8::/32", true)]
    [InlineData("::1/128", true)]
    public void IsValidCidr_ShouldReturnTrueForValidCidrs(string cidr, bool expected)
    {
        // Act
        var result = _service.IsValidCidr(cidr);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("192.168.1.0", false)]
    [InlineData("256.256.256.256/24", false)]
    [InlineData("192.168.1.0/33", false)]
    [InlineData("invalid", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsValidCidr_ShouldReturnFalseForInvalidCidrs(string cidr, bool expected)
    {
        // Act
        var result = _service.IsValidCidr(cidr);

        // Assert
        result.Should().Be(expected);
    }
}

public class DtoTests
{
    [Fact]
    public void AddressSpaceDto_ShouldInitializeCorrectly()
    {
        // Arrange & Act
        var dto = new AddressSpaceDto
        {
            Id = Guid.NewGuid(),
            Name = "Test Network",
            Description = "Test Description",
            CreatedOn = DateTimeOffset.UtcNow,
            ModifiedOn = DateTimeOffset.UtcNow
        };

        // Assert
        dto.Id.Should().NotBe(Guid.Empty);
        dto.Name.Should().Be("Test Network");
        dto.Description.Should().Be("Test Description");
    }

    [Fact]
    public void TagDefinitionDto_ShouldSupportBothTagTypes()
    {
        // Test Inheritable
        var inheritableTag = new TagDefinitionDto
        {
            AddressSpaceId = Guid.NewGuid(),
            Name = "Environment",
            Type = TagTypeDto.Inheritable
        };
        inheritableTag.Type.Should().Be(TagTypeDto.Inheritable);

        // Test NonInheritable
        var nonInheritableTag = new TagDefinitionDto
        {
            AddressSpaceId = Guid.NewGuid(),
            Name = "Owner",
            Type = TagTypeDto.NonInheritable
        };
        nonInheritableTag.Type.Should().Be(TagTypeDto.NonInheritable);
    }

    [Fact]
    public void IpCidrDto_ShouldStoreAllProperties()
    {
        // Arrange & Act
        var dto = new IpCidrDto
        {
            AddressSpaceId = Guid.NewGuid(),
            Id = Guid.NewGuid(),
            Prefix = "192.168.1.0/24",
            ParentId = Guid.NewGuid(),
            CreatedOn = DateTimeOffset.UtcNow,
            ModifiedOn = DateTimeOffset.UtcNow
        };

        // Assert
        dto.AddressSpaceId.Should().NotBe(Guid.Empty);
        dto.Id.Should().NotBe(Guid.Empty);
        dto.Prefix.Should().Be("192.168.1.0/24");
        dto.ParentId.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void TagAssignmentDto_ShouldSupportInheritance()
    {
        // Arrange & Act
        var assignment = new TagAssignmentDto
        {
            Name = "Environment",
            Value = "Production",
            IsInherited = true
        };

        // Assert
        assignment.Name.Should().Be("Environment");
        assignment.Value.Should().Be("Production");
        assignment.IsInherited.Should().BeTrue();
    }
}

// 测试枚举类型
public class EnumTests
{
    [Fact]
    public void TagType_ShouldHaveExpectedValues()
    {
        // Act & Assert
        var inheritableValue = TagType.Inheritable;
        var nonInheritableValue = TagType.NonInheritable;

        inheritableValue.Should().Be(TagType.Inheritable);
        nonInheritableValue.Should().Be(TagType.NonInheritable);

        // Test enum conversion
        ((int)TagType.Inheritable).Should().Be(0);
        ((int)TagType.NonInheritable).Should().Be(1);
    }

    [Fact]
    public void TagTypeDto_ShouldHaveExpectedValues()
    {
        // Act & Assert
        var inheritableValue = TagTypeDto.Inheritable;
        var nonInheritableValue = TagTypeDto.NonInheritable;

        inheritableValue.Should().Be(TagTypeDto.Inheritable);
        nonInheritableValue.Should().Be(TagTypeDto.NonInheritable);
    }
}

// 边界条件和异常情况测试
public class EdgeCaseTests
{
    [Fact]
    public void AddressSpace_ShouldHandleEmptyValues()
    {
        // Arrange & Act
        var addressSpace = new AddressSpace();

        // Assert
        addressSpace.Id.Should().Be(Guid.Empty);
        addressSpace.Name.Should().BeEmpty();
        addressSpace.Description.Should().BeNull();
    }

    [Fact]
    public void IpCidr_ShouldHandleNullParentId()
    {
        // Arrange & Act
        var ip = new IpCidr
        {
            Id = Guid.NewGuid(),
            AddressSpaceId = Guid.NewGuid(),
            Prefix = "192.168.1.0/24",
            ParentId = null
        };

        // Assert
        ip.ParentId.Should().BeNull();
    }

    [Fact]
    public void PaginationParameters_ShouldHandleNegativeValues()
    {
        // Arrange & Act
        var params1 = new PaginationParameters(-5, -10);

        // Assert
        params1.PageNumber.Should().Be(1);
        params1.PageSize.Should().Be(1);
    }

    [Fact]
    public async Task BasicCidrService_ShouldHandleConcurrentAccess()
    {
        // Arrange
        var service = new BasicCidrService();
        var tasks = new List<Task<bool>>();

        // Act - Test concurrent access
        for (int i = 0; i < 100; i++)
        {
            var cidr = $"192.168.{i % 255}.0/24";
            tasks.Add(Task.Run(() => service.IsValidCidr(cidr)));
        }

        var results = await Task.WhenAll(tasks);

        // Assert
        results.Should().AllSatisfy(r => r.Should().BeTrue());
    }
}
