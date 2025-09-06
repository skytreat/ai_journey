using FluentAssertions;
using IPAM.Contracts;

namespace Domain.Tests;

public class PaginatedResultTests
{
    [Fact]
    public void PaginatedResult_ShouldInitializeCorrectly()
    {
        // Arrange
        var items = new List<string> { "item1", "item2", "item3" };
        var totalCount = 100;
        var pageNumber = 2;
        var pageSize = 20;
        var totalPages = 5;

        // Act
        var result = new PaginatedResult<string>(items, totalCount, pageNumber, pageSize, totalPages);

        // Assert
        result.Items.Should().BeEquivalentTo(items);
        result.TotalCount.Should().Be(totalCount);
        result.PageNumber.Should().Be(pageNumber);
        result.PageSize.Should().Be(pageSize);
        result.TotalPages.Should().Be(totalPages);
    }

    [Fact]
    public void PaginatedResult_ShouldHandleEmptyItems()
    {
        // Arrange
        var items = new List<string>();

        // Act
        var result = new PaginatedResult<string>(items, 0, 1, 20, 0);

        // Assert
        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
        result.TotalPages.Should().Be(0);
    }
}

public class PaginationParametersTests
{
    [Fact]
    public void PaginationParameters_ShouldUseDefaultValues()
    {
        // Arrange & Act
        var parameters = new PaginationParameters();

        // Assert
        parameters.PageNumber.Should().Be(1);
        parameters.PageSize.Should().Be(20);
    }

    [Fact]
    public void PaginationParameters_ShouldSetCustomValues()
    {
        // Arrange & Act
        var parameters = new PaginationParameters(3, 50);

        // Assert
        parameters.PageNumber.Should().Be(3);
        parameters.PageSize.Should().Be(50);
    }

    [Theory]
    [InlineData(0, 1)] // PageNumber should be at least 1
    [InlineData(-5, 1)] // PageNumber should be at least 1
    public void PaginationParameters_ShouldEnforceMinimumPageNumber(int input, int expected)
    {
        // Arrange & Act
        var parameters = new PaginationParameters(input, 20);

        // Assert
        parameters.PageNumber.Should().Be(expected);
    }

    [Theory]
    [InlineData(0, 1)] // PageSize should be at least 1
    [InlineData(-10, 1)] // PageSize should be at least 1
    [InlineData(150, 100)] // PageSize should be at most 100
    public void PaginationParameters_ShouldEnforcePageSizeLimits(int input, int expected)
    {
        // Arrange & Act
        var parameters = new PaginationParameters(1, input);

        // Assert
        parameters.PageSize.Should().Be(expected);
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(10, 10)]
    [InlineData(50, 50)]
    [InlineData(100, 100)]
    public void PaginationParameters_ShouldAcceptValidPageSizes(int input, int expected)
    {
        // Arrange & Act
        var parameters = new PaginationParameters(1, input);

        // Assert
        parameters.PageSize.Should().Be(expected);
    }
}

public class AddressSpaceDtoTests
{
    [Fact]
    public void AddressSpaceDto_ShouldInitializeWithDefaults()
    {
        // Arrange & Act
        var dto = new AddressSpaceDto();

        // Assert
        dto.Id.Should().Be(Guid.Empty);
        dto.Name.Should().BeEmpty();
        dto.Description.Should().BeNull();
        dto.CreatedOn.Should().Be(default);
        dto.ModifiedOn.Should().Be(default);
    }

    [Fact]
    public void AddressSpaceDto_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var id = Guid.NewGuid();
        var name = "Test Address Space";
        var description = "Test Description";
        var createdOn = DateTimeOffset.UtcNow;
        var modifiedOn = DateTimeOffset.UtcNow.AddMinutes(5);

        // Act
        var dto = new AddressSpaceDto
        {
            Id = id,
            Name = name,
            Description = description,
            CreatedOn = createdOn,
            ModifiedOn = modifiedOn
        };

        // Assert
        dto.Id.Should().Be(id);
        dto.Name.Should().Be(name);
        dto.Description.Should().Be(description);
        dto.CreatedOn.Should().Be(createdOn);
        dto.ModifiedOn.Should().Be(modifiedOn);
    }
}

// DTO tests moved to SimplifiedTests.cs for better organization
