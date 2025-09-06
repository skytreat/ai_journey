using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Services.Frontend.Controllers;
using IPAM.Application;
using IPAM.Domain;

namespace Domain.Tests;

public class AddressSpacesControllerTests
{
    private readonly IAddressSpaceRepository _mockRepository;
    private readonly AddressSpacesController _controller;

    public AddressSpacesControllerTests()
    {
        _mockRepository = Substitute.For<IAddressSpaceRepository>();
        _controller = new AddressSpacesController(_mockRepository);
    }

    [Fact]
    public async Task Query_ShouldReturnPaginatedResults()
    {
        // Arrange
        var addressSpaces = new List<AddressSpace>
        {
            new() { Id = Guid.NewGuid(), Name = "AS1", Description = "Description 1" },
            new() { Id = Guid.NewGuid(), Name = "AS2", Description = "Description 2" },
            new() { Id = Guid.NewGuid(), Name = "AS3", Description = "Description 3" }
        };

        _mockRepository.QueryAsync(Arg.Any<string>(), Arg.Any<DateTimeOffset?>(), Arg.Any<DateTimeOffset?>(), Arg.Any<CancellationToken>())
            .Returns(addressSpaces);

        // Act
        var result = await _controller.Query(null, null, null, 1, 2, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        okResult.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task Get_WithValidId_ShouldReturnAddressSpace()
    {
        // Arrange
        var id = Guid.NewGuid();
        var addressSpace = new AddressSpace { Id = id, Name = "Test AS", Description = "Test Description" };
        _mockRepository.GetAsync(id, Arg.Any<CancellationToken>()).Returns(addressSpace);

        // Act
        var result = await _controller.Get(id, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        okResult.Value.Should().Be(addressSpace);
    }

    [Fact]
    public async Task Get_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var id = Guid.NewGuid();
        _mockRepository.GetAsync(id, Arg.Any<CancellationToken>()).Returns((AddressSpace?)null);

        // Act
        var result = await _controller.Get(id, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Create_WithValidInput_ShouldReturnCreatedResult()
    {
        // Arrange
        var input = new AddressSpacesController.AddressSpaceInput("Test AS", "Test Description");

        // Act
        var result = await _controller.Create(input, CancellationToken.None);

        // Assert
        result.Should().BeOfType<CreatedAtActionResult>();
        var createdResult = (CreatedAtActionResult)result;
        createdResult.ActionName.Should().Be(nameof(AddressSpacesController.Get));

        await _mockRepository.Received(1).CreateAsync(Arg.Any<AddressSpace>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Create_WithEmptyName_ShouldReturnBadRequest()
    {
        // Arrange
        var input = new AddressSpacesController.AddressSpaceInput("", "Test Description");

        // Act
        var result = await _controller.Create(input, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = (BadRequestObjectResult)result;
        badRequestResult.Value.Should().Be("Name is required.");
    }

    [Fact]
    public async Task Create_WithNullName_ShouldReturnBadRequest()
    {
        // Arrange
        var input = new AddressSpacesController.AddressSpaceInput(null, "Test Description");

        // Act
        var result = await _controller.Create(input, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Create_WithWhitespaceName_ShouldReturnBadRequest()
    {
        // Arrange
        var input = new AddressSpacesController.AddressSpaceInput("   ", "Test Description");

        // Act
        var result = await _controller.Create(input, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Update_WithValidInput_ShouldReturnNoContent()
    {
        // Arrange
        var id = Guid.NewGuid();
        var existingAddressSpace = new AddressSpace { Id = id, Name = "Old Name", Description = "Old Description" };
        var input = new AddressSpacesController.AddressSpaceInput("New Name", "New Description");

        _mockRepository.GetAsync(id, Arg.Any<CancellationToken>()).Returns(existingAddressSpace);

        // Act
        var result = await _controller.Update(id, input, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NoContentResult>();
        await _mockRepository.Received(1).UpdateAsync(Arg.Any<AddressSpace>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Update_WithNonExistentId_ShouldReturnNotFound()
    {
        // Arrange
        var id = Guid.NewGuid();
        var input = new AddressSpacesController.AddressSpaceInput("New Name", "New Description");

        _mockRepository.GetAsync(id, Arg.Any<CancellationToken>()).Returns((AddressSpace?)null);

        // Act
        var result = await _controller.Update(id, input, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
        await _mockRepository.DidNotReceive().UpdateAsync(Arg.Any<AddressSpace>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Update_WithEmptyName_ShouldReturnBadRequest()
    {
        // Arrange
        var id = Guid.NewGuid();
        var input = new AddressSpacesController.AddressSpaceInput("", "New Description");

        // Act
        var result = await _controller.Update(id, input, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Delete_ShouldCallRepository()
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act
        var result = await _controller.Delete(id, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NoContentResult>();
        await _mockRepository.Received(1).DeleteAsync(id, Arg.Any<CancellationToken>());
    }
}

public class TagsControllerTests
{
    private readonly ITagRepository _mockRepository;
    private readonly TagsController _controller;

    public TagsControllerTests()
    {
        _mockRepository = Substitute.For<ITagRepository>();
        _controller = new TagsController(_mockRepository);
    }

    [Fact]
    public async Task Query_ShouldReturnPaginatedResults()
    {
        // Arrange
        var addressSpaceId = Guid.NewGuid();
        var tags = new List<TagDefinition>
        {
            new() { AddressSpaceId = addressSpaceId, Name = "Tag1", Type = TagType.Inheritable },
            new() { AddressSpaceId = addressSpaceId, Name = "Tag2", Type = TagType.NonInheritable }
        };

        _mockRepository.QueryAsync(addressSpaceId, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(tags);

        // Act
        var result = await _controller.Query(addressSpaceId, null, 1, 2, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Get_WithValidNameAndAddressSpaceId_ShouldReturnTag()
    {
        // Arrange
        var addressSpaceId = Guid.NewGuid();
        var tagName = "Environment";
        var tag = new TagDefinition { AddressSpaceId = addressSpaceId, Name = tagName, Type = TagType.Inheritable };
        
        _mockRepository.GetAsync(addressSpaceId, tagName, Arg.Any<CancellationToken>()).Returns(tag);

        // Act
        var result = await _controller.Get(addressSpaceId, tagName, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        okResult.Value.Should().Be(tag);
    }

    [Fact]
    public async Task Get_WithInvalidName_ShouldReturnNotFound()
    {
        // Arrange
        var addressSpaceId = Guid.NewGuid();
        var tagName = "NonExistent";
        
        _mockRepository.GetAsync(addressSpaceId, tagName, Arg.Any<CancellationToken>()).Returns((TagDefinition?)null);

        // Act
        var result = await _controller.Get(addressSpaceId, tagName, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Upsert_WithValidInput_ShouldReturnNoContent()
    {
        // Arrange
        var addressSpaceId = Guid.NewGuid();
        var input = new TagsController.TagInput("Environment", "Environment tag", "Inheritable");

        // Act
        var result = await _controller.Upsert(addressSpaceId, input, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NoContentResult>();
        await _mockRepository.Received(1).UpsertAsync(Arg.Any<TagDefinition>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Upsert_WithEmptyName_ShouldReturnBadRequest()
    {
        // Arrange
        var addressSpaceId = Guid.NewGuid();
        var input = new TagsController.TagInput("", "Environment tag", "Inheritable");

        // Act
        var result = await _controller.Upsert(addressSpaceId, input, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = (BadRequestObjectResult)result;
        badRequestResult.Value.Should().Be("Tag name is required.");
    }

    [Fact]
    public async Task Upsert_WithInvalidTagType_ShouldReturnBadRequest()
    {
        // Arrange
        var addressSpaceId = Guid.NewGuid();
        var input = new TagsController.TagInput("Environment", "Environment tag", "InvalidType");

        // Act
        var result = await _controller.Upsert(addressSpaceId, input, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = (BadRequestObjectResult)result;
        badRequestResult.Value.Should().Be("Invalid tag type.");
    }

    [Fact]
    public async Task Delete_ShouldCallRepository()
    {
        // Arrange
        var addressSpaceId = Guid.NewGuid();
        var tagName = "Environment";

        // Act
        var result = await _controller.Delete(addressSpaceId, tagName, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NoContentResult>();
        await _mockRepository.Received(1).DeleteAsync(addressSpaceId, tagName, Arg.Any<CancellationToken>());
    }
}

public class IpControllerTests
{
    private readonly IIpRepository _mockRepository;
    private readonly IpController _controller;

    public IpControllerTests()
    {
        _mockRepository = Substitute.For<IIpRepository>();
        _controller = new IpController(_mockRepository);
    }

    [Fact]
    public async Task GetById_WithValidId_ShouldReturnIp()
    {
        // Arrange
        var addressSpaceId = Guid.NewGuid();
        var id = Guid.NewGuid();
        var ip = new IpCidr { Id = id, AddressSpaceId = addressSpaceId, Prefix = "192.168.1.0/24" };
        
        _mockRepository.GetByIdAsync(addressSpaceId, id, Arg.Any<CancellationToken>()).Returns(ip);

        // Act
        var result = await _controller.GetById(addressSpaceId, id, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        okResult.Value.Should().Be(ip);
    }

    [Fact]
    public async Task GetById_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var addressSpaceId = Guid.NewGuid();
        var id = Guid.NewGuid();
        
        _mockRepository.GetByIdAsync(addressSpaceId, id, Arg.Any<CancellationToken>()).Returns((IpCidr?)null);

        // Act
        var result = await _controller.GetById(addressSpaceId, id, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
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
    public async Task Query_ShouldReturnPaginatedResults()
    {
        // Arrange
        var addressSpaceId = Guid.NewGuid();
        var ips = new List<IpCidr>
        {
            new() { Id = Guid.NewGuid(), AddressSpaceId = addressSpaceId, Prefix = "192.168.1.0/24" },
            new() { Id = Guid.NewGuid(), AddressSpaceId = addressSpaceId, Prefix = "192.168.2.0/24" }
        };

        _mockRepository.QueryByTagsAsync(addressSpaceId, Arg.Any<Dictionary<string, string>>(), Arg.Any<CancellationToken>())
            .Returns(ips);

        // Act
        var result = await _controller.Query(addressSpaceId, null, null, 1, 2, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Upsert_WithValidInput_ShouldReturnOk()
    {
        // Arrange
        var addressSpaceId = Guid.NewGuid();
        var input = new IpController.IpInput("192.168.1.0/24");

        // Act
        var result = await _controller.Upsert(addressSpaceId, input, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        await _mockRepository.Received(1).UpsertAsync(Arg.Any<IpCidr>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Upsert_WithEmptyPrefix_ShouldReturnBadRequest()
    {
        // Arrange
        var addressSpaceId = Guid.NewGuid();
        var input = new IpController.IpInput("");

        // Act
        var result = await _controller.Upsert(addressSpaceId, input, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = (BadRequestObjectResult)result;
        badRequestResult.Value.Should().Be("Valid CIDR prefix is required.");
    }

    [Fact]
    public async Task Update_WithValidInput_ShouldReturnNoContent()
    {
        // Arrange
        var addressSpaceId = Guid.NewGuid();
        var id = Guid.NewGuid();
        var existingIp = new IpCidr { Id = id, AddressSpaceId = addressSpaceId, Prefix = "192.168.1.0/24" };
        var input = new IpController.IpInput("192.168.2.0/24");

        _mockRepository.GetByIdAsync(addressSpaceId, id, Arg.Any<CancellationToken>()).Returns(existingIp);

        // Act
        var result = await _controller.Update(addressSpaceId, id, input, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NoContentResult>();
        await _mockRepository.Received(1).UpsertAsync(Arg.Any<IpCidr>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Update_WithNonExistentId_ShouldReturnNotFound()
    {
        // Arrange
        var addressSpaceId = Guid.NewGuid();
        var id = Guid.NewGuid();
        var input = new IpController.IpInput("192.168.2.0/24");

        _mockRepository.GetByIdAsync(addressSpaceId, id, Arg.Any<CancellationToken>()).Returns((IpCidr?)null);

        // Act
        var result = await _controller.Update(addressSpaceId, id, input, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
        await _mockRepository.DidNotReceive().UpsertAsync(Arg.Any<IpCidr>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Delete_ShouldCallRepository()
    {
        // Arrange
        var addressSpaceId = Guid.NewGuid();
        var id = Guid.NewGuid();

        // Act
        var result = await _controller.Delete(addressSpaceId, id, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NoContentResult>();
        await _mockRepository.Received(1).DeleteAsync(addressSpaceId, id, Arg.Any<CancellationToken>());
    }
}
