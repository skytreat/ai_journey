using FluentAssertions;
using IPAM.Domain;
using IPAM.Infrastructure;
using IPAM.Application;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Domain.Tests;

public class SimpleIntegrationTests
{
    private readonly IServiceProvider _serviceProvider;

    public SimpleIntegrationTests()
    {
        var services = new ServiceCollection();
        
        // Add configuration
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["ConnectionStrings:AzureTableStorage"] = "UseDevelopmentStorage=true"
            })
            .Build();
        
        services.AddSingleton<IConfiguration>(configuration);
        
        // Add core services
        services.AddSingleton<ICidrService, BasicCidrService>();
        services.AddSingleton<ITagPolicyService, TagPolicyService>();
        
        _serviceProvider = services.BuildServiceProvider();
    }

    [Fact]
    public void CidrService_Integration_ShouldValidateCorrectly()
    {
        // Arrange
        var cidrService = _serviceProvider.GetRequiredService<ICidrService>();

        // Act & Assert
        cidrService.IsValidCidr("192.168.1.0/24").Should().BeTrue();
        cidrService.IsValidCidr("10.0.0.0/8").Should().BeTrue();
        cidrService.IsValidCidr("172.16.0.0/12").Should().BeTrue();
        cidrService.IsValidCidr("invalid-cidr").Should().BeFalse();
        cidrService.IsValidCidr("").Should().BeFalse();
        cidrService.IsValidCidr(null).Should().BeFalse();
    }

    [Fact]
    public void CidrService_Integration_ShouldCalculateHierarchyCorrectly()
    {
        // Arrange
        var cidrService = _serviceProvider.GetRequiredService<ICidrService>();

        // Act & Assert
        cidrService.IsParent("192.168.0.0/16", "192.168.1.0/24").Should().BeTrue();
        cidrService.IsParent("10.0.0.0/8", "10.1.0.0/16").Should().BeTrue();
        cidrService.IsParent("172.16.0.0/12", "172.16.1.0/24").Should().BeTrue();
        
        // Same network should not be parent of itself
        cidrService.IsParent("192.168.1.0/24", "192.168.1.0/24").Should().BeFalse();
        
        // Smaller network cannot be parent of larger network
        cidrService.IsParent("192.168.1.0/24", "192.168.0.0/16").Should().BeFalse();
    }

    [Fact]
    public void TagPolicyService_Integration_ShouldValidateAssignmentsCorrectly()
    {
        // Arrange
        var tagPolicyService = _serviceProvider.GetRequiredService<ITagPolicyService>();
        
        var tagDefinition = new TagDefinition
        {
            AddressSpaceId = Guid.NewGuid(),
            Name = "Environment",
            Type = TagType.Inheritable,
            KnownValues = new List<string> { "Production", "Development", "Testing" }
        };

        var validAssignment = new TagAssignment
        {
            Name = "Environment",
            Value = "Production"
        };

        var invalidAssignment = new TagAssignment
        {
            Name = "Environment",
            Value = "InvalidValue"
        };

        // Act & Assert
        tagPolicyService.ValidateAssignment(tagDefinition, validAssignment.Value);
        
        // Invalid value should throw
        Action invalidAction = () => tagPolicyService.ValidateAssignment(tagDefinition, invalidAssignment.Value);
        invalidAction.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void DomainModels_Integration_ShouldWorkTogether()
    {
        // Arrange
        var addressSpace = new AddressSpace
        {
            Id = Guid.NewGuid(),
            Name = "Production Network",
            Description = "Production environment network",
            CreatedOn = DateTimeOffset.UtcNow,
            ModifiedOn = DateTimeOffset.UtcNow
        };

        var tagDefinition = new TagDefinition
        {
            AddressSpaceId = addressSpace.Id,
            Name = "Environment",
            Type = TagType.Inheritable,
            KnownValues = new List<string> { "Production", "Development" },
            CreatedOn = DateTimeOffset.UtcNow,
            ModifiedOn = DateTimeOffset.UtcNow
        };

        var ipCidr = new IpCidr
        {
            Id = Guid.NewGuid(),
            AddressSpaceId = addressSpace.Id,
            Prefix = "192.168.1.0/24",
            CreatedOn = DateTimeOffset.UtcNow,
            ModifiedOn = DateTimeOffset.UtcNow
        };

        var tagAssignment = new TagAssignment
        {
            Name = "Environment",
            Value = "Production"
        };

        // Act & Assert
        addressSpace.Id.Should().NotBeEmpty();
        addressSpace.Name.Should().Be("Production Network");


        tagDefinition.AddressSpaceId.Should().Be(addressSpace.Id);
        tagDefinition.Name.Should().Be("Environment");
        tagDefinition.Type.Should().Be(TagType.Inheritable);

        ipCidr.AddressSpaceId.Should().Be(addressSpace.Id);
        ipCidr.Prefix.Should().Be("192.168.1.0/24");

        tagAssignment.Name.Should().Be("Environment");
        tagAssignment.Value.Should().Be("Production");
    }

    [Fact]
    public void BusinessLogic_Integration_ShouldEnforceRules()
    {
        // Arrange
        var cidrService = _serviceProvider.GetRequiredService<ICidrService>();
        var tagPolicyService = _serviceProvider.GetRequiredService<ITagPolicyService>();

        // Test CIDR validation
        var validCidrs = new[] { "192.168.1.0/24", "10.0.0.0/8", "172.16.0.0/12" };
        var invalidCidrs = new[] { "invalid", "192.168.1.0", "256.256.256.256/24" };

        // Act & Assert - CIDR validation
        foreach (var cidr in validCidrs)
        {
            cidrService.IsValidCidr(cidr).Should().BeTrue($"CIDR {cidr} should be valid");
        }

        foreach (var cidr in invalidCidrs)
        {
            cidrService.IsValidCidr(cidr).Should().BeFalse($"CIDR {cidr} should be invalid");
        }

        // Test hierarchy validation
        cidrService.IsParent("192.168.0.0/16", "192.168.1.0/24").Should().BeTrue();
        cidrService.IsParent("192.168.1.0/24", "192.168.0.0/16").Should().BeFalse();
    }

    [Fact]
    public void DataFlow_Integration_ShouldHandleComplexScenarios()
    {
        // Arrange
        var cidrService = _serviceProvider.GetRequiredService<ICidrService>();
        
        // Create a complex network hierarchy
        var networks = new[]
        {
            "10.0.0.0/8",      // Large network
            "10.1.0.0/16",     // Subnet of 10.0.0.0/8
            "10.1.1.0/24",     // Subnet of 10.1.0.0/16
            "10.1.2.0/24",     // Another subnet of 10.1.0.0/16
            "10.2.0.0/16",     // Another subnet of 10.0.0.0/8
            "192.168.0.0/16",  // Separate network
            "192.168.1.0/24"   // Subnet of 192.168.0.0/16
        };

        // Act & Assert - Validate all networks
        foreach (var network in networks)
        {
            cidrService.IsValidCidr(network).Should().BeTrue($"Network {network} should be valid");
        }

        // Test hierarchy relationships
        cidrService.IsParent("10.0.0.0/8", "10.1.0.0/16").Should().BeTrue();
        cidrService.IsParent("10.1.0.0/16", "10.1.1.0/24").Should().BeTrue();
        cidrService.IsParent("10.0.0.0/8", "10.1.1.0/24").Should().BeTrue();
        
        // Test non-hierarchical relationships
        cidrService.IsParent("10.1.1.0/24", "10.1.2.0/24").Should().BeFalse();
        cidrService.IsParent("10.0.0.0/8", "192.168.0.0/16").Should().BeFalse();
    }
}
