using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Services.Frontend.Controllers;
using IPAM.Application;
using IPAM.Domain;
using IPAM.Infrastructure;

namespace Domain.Tests;

public class ExtensiveAddressSpacesControllerTests
{
    private readonly IAddressSpaceRepository _mockRepository;
    private readonly AddressSpacesController _controller;

    public ExtensiveAddressSpacesControllerTests()
    {
        _mockRepository = Substitute.For<IAddressSpaceRepository>();
        _controller = new AddressSpacesController(_mockRepository);
    }

    [Fact]
    public async Task Query_WithFilters_ShouldCallRepositoryWithCorrectParameters()
    {
        // Arrange
        var name = "Test";
        var createdAfter = DateTimeOffset.UtcNow.AddDays(-7);
        var createdBefore = DateTimeOffset.UtcNow;
        var mockResults = new List<AddressSpace>();
        
        _mockRepository.QueryAsync(name, createdAfter, createdBefore, Arg.Any<CancellationToken>())
            .Returns(mockResults);

        // Act
        var result = await _controller.Query(name, createdAfter, createdBefore, 1, 20, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        await _mockRepository.Received(1).QueryAsync(name, createdAfter, createdBefore, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Query_WithLargePageSize_ShouldClampToMaximum()
    {
        // Arrange
        _mockRepository.QueryAsync(Arg.Any<string>(), Arg.Any<DateTimeOffset?>(), Arg.Any<DateTimeOffset?>(), Arg.Any<CancellationToken>())
            .Returns(new List<AddressSpace>());

        // Act
        var result = await _controller.Query(null, null, null, 1, 200, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        okResult.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task Create_WithWhitespaceOnlyName_ShouldReturnBadRequest()
    {
        // Arrange
        var input = new AddressSpacesController.AddressSpaceInput("   ", "Description");

        // Act
        var result = await _controller.Create(input, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Create_WithValidInput_ShouldGenerateIdAndTimestamps()
    {
        // Arrange
        var input = new AddressSpacesController.AddressSpaceInput("Valid Name", "Description");
        AddressSpace? capturedAddressSpace = null;
        
        _mockRepository.When(x => x.CreateAsync(Arg.Any<AddressSpace>(), Arg.Any<CancellationToken>()))
            .Do(x => capturedAddressSpace = x.Arg<AddressSpace>());

        // Act
        var result = await _controller.Create(input, CancellationToken.None);

        // Assert
        result.Should().BeOfType<CreatedAtActionResult>();
        capturedAddressSpace.Should().NotBeNull();
        capturedAddressSpace!.Id.Should().NotBe(Guid.Empty);
        capturedAddressSpace.Name.Should().Be("Valid Name");
        capturedAddressSpace.Description.Should().Be("Description");
        capturedAddressSpace.CreatedOn.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromMinutes(1));
        capturedAddressSpace.ModifiedOn.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task Update_WithValidInput_ShouldUpdateTimestamp()
    {
        // Arrange
        var id = Guid.NewGuid();
        var existingAddressSpace = new AddressSpace
        {
            Id = id,
            Name = "Old Name",
            Description = "Old Description",
            CreatedOn = DateTimeOffset.UtcNow.AddDays(-1),
            ModifiedOn = DateTimeOffset.UtcNow.AddDays(-1)
        };
        var input = new AddressSpacesController.AddressSpaceInput("New Name", "New Description");
        AddressSpace? capturedAddressSpace = null;

        _mockRepository.GetAsync(id, Arg.Any<CancellationToken>()).Returns(existingAddressSpace);
        _mockRepository.When(x => x.UpdateAsync(Arg.Any<AddressSpace>(), Arg.Any<CancellationToken>()))
            .Do(x => capturedAddressSpace = x.Arg<AddressSpace>());

        // Act
        var result = await _controller.Update(id, input, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NoContentResult>();
        capturedAddressSpace.Should().NotBeNull();
        capturedAddressSpace!.Name.Should().Be("New Name");
        capturedAddressSpace.Description.Should().Be("New Description");
        capturedAddressSpace.ModifiedOn.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromMinutes(1));
        capturedAddressSpace.CreatedOn.Should().Be(existingAddressSpace.CreatedOn); // Should not change
    }
}

public class ExtensiveTagsControllerTests
{
    private readonly ITagRepository _mockRepository;
    private readonly TagsController _controller;

    public ExtensiveTagsControllerTests()
    {
        _mockRepository = Substitute.For<ITagRepository>();
        _controller = new TagsController(_mockRepository);
    }

    [Theory]
    [InlineData("Inheritable")]
    [InlineData("NonInheritable")]
    public async Task Upsert_WithValidTagType_ShouldSucceed(string tagType)
    {
        // Arrange
        var addressSpaceId = Guid.NewGuid();
        var input = new TagsController.TagInput("Environment", "Environment tag", tagType);

        // Act
        var result = await _controller.Upsert(addressSpaceId, input, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NoContentResult>();
        await _mockRepository.Received(1).UpsertAsync(Arg.Any<TagDefinition>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Upsert_WithEmptyDescription_ShouldStillSucceed()
    {
        // Arrange
        var addressSpaceId = Guid.NewGuid();
        var input = new TagsController.TagInput("Environment", "", "Inheritable");

        // Act
        var result = await _controller.Upsert(addressSpaceId, input, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Upsert_WithNullDescription_ShouldStillSucceed()
    {
        // Arrange
        var addressSpaceId = Guid.NewGuid();
        var input = new TagsController.TagInput("Environment", null, "Inheritable");

        // Act
        var result = await _controller.Upsert(addressSpaceId, input, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Upsert_ShouldSetTimestamps()
    {
        // Arrange
        var addressSpaceId = Guid.NewGuid();
        var input = new TagsController.TagInput("Environment", "Environment tag", "Inheritable");
        TagDefinition? capturedTag = null;

        _mockRepository.When(x => x.UpsertAsync(Arg.Any<TagDefinition>(), Arg.Any<CancellationToken>()))
            .Do(x => capturedTag = x.Arg<TagDefinition>());

        // Act
        await _controller.Upsert(addressSpaceId, input, CancellationToken.None);

        // Assert
        capturedTag.Should().NotBeNull();
        capturedTag!.AddressSpaceId.Should().Be(addressSpaceId);
        capturedTag.Name.Should().Be("Environment");
        capturedTag.Type.Should().Be(TagType.Inheritable);
        capturedTag.CreatedOn.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromMinutes(1));
        capturedTag.ModifiedOn.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task Query_WithNameFilter_ShouldCallRepository()
    {
        // Arrange
        var addressSpaceId = Guid.NewGuid();
        var nameFilter = "Env";
        _mockRepository.QueryAsync(addressSpaceId, nameFilter, Arg.Any<CancellationToken>())
            .Returns(new List<TagDefinition>());

        // Act
        var result = await _controller.Query(addressSpaceId, nameFilter, 1, 20, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        await _mockRepository.Received(1).QueryAsync(addressSpaceId, nameFilter, Arg.Any<CancellationToken>());
    }
}

public class ExtensiveIpControllerTests
{
    private readonly IIpRepository _mockRepository;
    private readonly ICidrService _mockCidrService;
    private readonly IpController _controller;

    public ExtensiveIpControllerTests()
    {
        _mockRepository = Substitute.For<IIpRepository>();
        _mockCidrService = Substitute.For<ICidrService>();
        _controller = new IpController(_mockRepository);
    }

    [Theory]
    [InlineData("192.168.1.0/24")]
    [InlineData("10.0.0.0/8")]
    [InlineData("2001:db8::/32")]
    public async Task Upsert_WithValidCidr_ShouldSucceed(string cidr)
    {
        // Arrange
        var addressSpaceId = Guid.NewGuid();
        var input = new IpController.IpInput(cidr);

        // Act
        var result = await _controller.Upsert(addressSpaceId, input, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        await _mockRepository.Received(1).UpsertAsync(Arg.Any<IpCidr>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Upsert_ShouldGenerateIdAndTimestamps()
    {
        // Arrange
        var addressSpaceId = Guid.NewGuid();
        var input = new IpController.IpInput("192.168.1.0/24");
        IpCidr? capturedIp = null;

        _mockRepository.When(x => x.UpsertAsync(Arg.Any<IpCidr>(), Arg.Any<CancellationToken>()))
            .Do(x => capturedIp = x.Arg<IpCidr>());

        // Act
        await _controller.Upsert(addressSpaceId, input, CancellationToken.None);

        // Assert
        capturedIp.Should().NotBeNull();
        capturedIp!.Id.Should().NotBe(Guid.Empty);
        capturedIp.AddressSpaceId.Should().Be(addressSpaceId);
        capturedIp.Prefix.Should().Be("192.168.1.0/24");
        capturedIp.CreatedOn.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromMinutes(1));
        capturedIp.ModifiedOn.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task Update_WithExistingIp_ShouldUpdateTimestamp()
    {
        // Arrange
        var addressSpaceId = Guid.NewGuid();
        var id = Guid.NewGuid();
        var existingIp = new IpCidr
        {
            Id = id,
            AddressSpaceId = addressSpaceId,
            Prefix = "192.168.1.0/24",
            CreatedOn = DateTimeOffset.UtcNow.AddDays(-1),
            ModifiedOn = DateTimeOffset.UtcNow.AddDays(-1)
        };
        var input = new IpController.IpInput("192.168.2.0/24");
        IpCidr? capturedIp = null;

        _mockRepository.GetByIdAsync(addressSpaceId, id, Arg.Any<CancellationToken>()).Returns(existingIp);
        _mockRepository.When(x => x.UpsertAsync(Arg.Any<IpCidr>(), Arg.Any<CancellationToken>()))
            .Do(x => capturedIp = x.Arg<IpCidr>());

        // Act
        var result = await _controller.Update(addressSpaceId, id, input, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NoContentResult>();
        capturedIp.Should().NotBeNull();
        capturedIp!.Prefix.Should().Be("192.168.2.0/24");
        capturedIp.ModifiedOn.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromMinutes(1));
        capturedIp.CreatedOn.Should().Be(existingIp.CreatedOn); // Should not change
    }

    [Fact]
    public async Task Query_WithTagFilters_ShouldCallRepository()
    {
        // Arrange
        var addressSpaceId = Guid.NewGuid();
        var tagName = "Environment";
        var tagValue = "Production";
        _mockRepository.QueryByTagsAsync(addressSpaceId, Arg.Any<Dictionary<string, string>>(), Arg.Any<CancellationToken>())
            .Returns(new List<IpCidr>());

        // Act
        var result = await _controller.Query(addressSpaceId, tagName, tagValue, 1, 20, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        await _mockRepository.Received(1).QueryByTagsAsync(
            addressSpaceId,
            Arg.Is<Dictionary<string, string>>(d => d.ContainsKey(tagName) && d[tagName] == tagValue),
            Arg.Any<CancellationToken>()
        );
    }

    [Fact]
    public async Task Query_WithoutTagFilters_ShouldCallRepositoryWithEmptyDictionary()
    {
        // Arrange
        var addressSpaceId = Guid.NewGuid();
        _mockRepository.QueryByTagsAsync(addressSpaceId, Arg.Any<Dictionary<string, string>>(), Arg.Any<CancellationToken>())
            .Returns(new List<IpCidr>());

        // Act
        var result = await _controller.Query(addressSpaceId, null, null, 1, 20, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        await _mockRepository.Received(1).QueryByTagsAsync(
            addressSpaceId,
            Arg.Is<Dictionary<string, string>>(d => d.Count == 0),
            Arg.Any<CancellationToken>()
        );
    }

    [Fact]
    public async Task GetByCidr_WithValidCidr_ShouldReturnIp()
    {
        // Arrange
        var addressSpaceId = Guid.NewGuid();
        var cidr = "192.168.1.0/24";
        var ip = new IpCidr { Id = Guid.NewGuid(), AddressSpaceId = addressSpaceId, Prefix = cidr };
        
        _mockRepository.GetByCidrAsync(addressSpaceId, cidr, Arg.Any<CancellationToken>()).Returns(ip);

        // Act
        var result = await _controller.GetByCidr(addressSpaceId, cidr, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        okResult.Value.Should().Be(ip);
    }

    [Fact]
    public async Task GetByCidr_WithNonExistentCidr_ShouldReturnNotFound()
    {
        // Arrange
        var addressSpaceId = Guid.NewGuid();
        var cidr = "192.168.1.0/24";
        
        _mockRepository.GetByCidrAsync(addressSpaceId, cidr, Arg.Any<CancellationToken>()).Returns((IpCidr?)null);

        // Act
        var result = await _controller.GetByCidr(addressSpaceId, cidr, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }
}
