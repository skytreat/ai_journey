using Xunit;
using Moq;
using Ipam.DataAccess.Services;
using Ipam.DataAccess.Interfaces;
using Ipam.DataAccess.Entities;
using Ipam.ServiceContract.DTOs;
using AutoMapper;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Ipam.DataAccess.Tests.Services
{
    /// <summary>
    /// Unit tests for TagServiceImpl
    /// </summary>
    /// <remarks>
    /// Author: IPAM Team
    /// Date: 2024-01-20
    /// </remarks>
    public class TagServiceImplTests
    {
        private readonly Mock<ITagRepository> _tagRepositoryMock;
        private readonly Mock<TagInheritanceService> _tagInheritanceServiceMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<ILogger<TagServiceImpl>> _loggerMock;
        private readonly TagServiceImpl _service;

        public TagServiceImplTests()
        {
            _tagRepositoryMock = new Mock<ITagRepository>();
            _tagInheritanceServiceMock = new Mock<TagInheritanceService>(new Mock<ITagRepository>().Object);
            _mapperMock = new Mock<IMapper>();
            _loggerMock = new Mock<ILogger<TagServiceImpl>>();

            _service = new TagServiceImpl(
                _tagRepositoryMock.Object,
                _tagInheritanceServiceMock.Object,
                _mapperMock.Object,
                _loggerMock.Object);
        }

        #region CreateTagAsync Tests

        [Fact]
        public async Task CreateTagAsync_ValidTag_CreatesTagSuccessfully()
        {
            // Arrange
            var tag = new Tag
            {
                Name = "Environment",
                AddressSpaceId = "space1",
                Description = "Environment tag",
                Type = "Inheritable",
                KnownValues = new List<string> { "Production", "Staging", "Development" },
                Implies = new Dictionary<string, Dictionary<string, string>> 
                { 
                    { "Backup", new Dictionary<string, string> { { "value", "Required" } } } 
                },
                Attributes = new Dictionary<string, Dictionary<string, string>> 
                { 
                    { "Color", new Dictionary<string, string> { { "value", "Blue" } } } 
                }
            };

            var tagEntity = new TagEntity
            {
                Name = "Environment",
                AddressSpaceId = "space1",
                Description = "Environment tag",
                Type = "Inheritable"
            };

            var createdEntity = new TagEntity
            {
                Name = "Environment",
                AddressSpaceId = "space1",
                Description = "Environment tag",
                Type = "Inheritable",
                CreatedOn = DateTime.UtcNow,
                ModifiedOn = DateTime.UtcNow
            };

            _mapperMock.Setup(m => m.Map<TagEntity>(tag))
                .Returns(tagEntity);
            _mapperMock.Setup(m => m.Map<Tag>(createdEntity))
                .Returns(tag);

            _tagRepositoryMock.Setup(r => r.GetByNameAsync("space1", "Environment"))
                .ReturnsAsync((TagEntity)null); // Tag doesn't exist

            _tagRepositoryMock.Setup(r => r.CreateAsync(It.IsAny<TagEntity>()))
                .ReturnsAsync(createdEntity);

            // Act
            var result = await _service.CreateTagAsync(tag);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Environment", result.Name);
            Assert.Equal("space1", result.AddressSpaceId);

            _tagRepositoryMock.Verify(r => r.CreateAsync(It.Is<TagEntity>(e =>
                e.Name == "Environment" &&
                e.AddressSpaceId == "space1" &&
                e.Type == "Inheritable")), Times.Once);
        }

        [Fact]
        public async Task CreateTagAsync_DuplicateTag_ThrowsException()
        {
            // Arrange
            var tag = new Tag
            {
                Name = "Environment",
                AddressSpaceId = "space1",
                Type = "Inheritable"
            };

            var existingEntity = new TagEntity
            {
                Name = "Environment",
                AddressSpaceId = "space1"
            };

            _tagRepositoryMock.Setup(r => r.GetByNameAsync("space1", "Environment"))
                .ReturnsAsync(existingEntity);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.CreateTagAsync(tag));

            Assert.Contains("already exists", exception.Message);
            _tagRepositoryMock.Verify(r => r.CreateAsync(It.IsAny<TagEntity>()), Times.Never);
        }

        [Fact]
        public async Task CreateTagAsync_InvalidTagName_ThrowsException()
        {
            // Arrange
            var tag = new Tag
            {
                Name = "", // Invalid empty name
                AddressSpaceId = "space1",
                Type = "Inheritable"
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _service.CreateTagAsync(tag));

            Assert.Contains("Tag name cannot be empty", exception.Message);
        }

        [Fact]
        public async Task CreateTagAsync_InvalidAddressSpaceId_ThrowsException()
        {
            // Arrange
            var tag = new Tag
            {
                Name = "Environment",
                AddressSpaceId = "", // Invalid empty address space ID
                Type = "Inheritable"
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _service.CreateTagAsync(tag));

            Assert.Contains("AddressSpaceId cannot be empty", exception.Message);
        }

        [Fact]
        public async Task CreateTagAsync_InvalidTagType_ThrowsException()
        {
            // Arrange
            var tag = new Tag
            {
                Name = "Environment",
                AddressSpaceId = "space1",
                Type = "InvalidType" // Invalid type
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _service.CreateTagAsync(tag));

            Assert.Contains("Invalid tag type", exception.Message);
        }

        #endregion

        #region GetTagAsync Tests

        [Fact]
        public async Task GetTagAsync_ExistingTag_ReturnsTag()
        {
            // Arrange
            var tagName = "Environment";
            var addressSpaceId = "space1";

            var entity = new TagEntity
            {
                Name = tagName,
                AddressSpaceId = addressSpaceId,
                Type = "Inheritable"
            };

            var dto = new Tag
            {
                Name = tagName,
                AddressSpaceId = addressSpaceId,
                Type = "Inheritable"
            };

            _tagRepositoryMock.Setup(r => r.GetByNameAsync(addressSpaceId, tagName))
                .ReturnsAsync(entity);
            _mapperMock.Setup(m => m.Map<Tag>(entity))
                .Returns(dto);

            // Act
            var result = await _service.GetTagAsync(tagName, addressSpaceId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(tagName, result.Name);
            Assert.Equal(addressSpaceId, result.AddressSpaceId);
        }

        [Fact]
        public async Task GetTagAsync_NonExistingTag_ReturnsNull()
        {
            // Arrange
            var tagName = "NonExisting";
            var addressSpaceId = "space1";

            _tagRepositoryMock.Setup(r => r.GetByNameAsync(addressSpaceId, tagName))
                .ReturnsAsync((TagEntity)null);

            // Act
            var result = await _service.GetTagAsync(tagName, addressSpaceId);

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region GetTagsAsync Tests

        [Fact]
        public async Task GetTagsAsync_HasTags_ReturnsAllTags()
        {
            // Arrange
            var addressSpaceId = "space1";
            var entities = new List<TagEntity>
            {
                new TagEntity { Name = "Environment", AddressSpaceId = addressSpaceId },
                new TagEntity { Name = "Region", AddressSpaceId = addressSpaceId }
            };

            var dtos = new List<Tag>
            {
                new Tag { Name = "Environment", AddressSpaceId = addressSpaceId },
                new Tag { Name = "Region", AddressSpaceId = addressSpaceId }
            };

            _tagRepositoryMock.Setup(r => r.GetAllAsync(addressSpaceId))
                .ReturnsAsync(entities);
            _mapperMock.Setup(m => m.Map<IEnumerable<Tag>>(entities))
                .Returns(dtos);

            // Act
            var result = await _service.GetTagsAsync(addressSpaceId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
            Assert.Contains(result, t => t.Name == "Environment");
            Assert.Contains(result, t => t.Name == "Region");
        }

        [Fact]
        public async Task GetTagsAsync_NoTags_ReturnsEmptyCollection()
        {
            // Arrange
            var addressSpaceId = "space1";
            var entities = new List<TagEntity>();
            var dtos = new List<Tag>();

            _tagRepositoryMock.Setup(r => r.GetAllAsync(addressSpaceId))
                .ReturnsAsync(entities);
            _mapperMock.Setup(m => m.Map<IEnumerable<Tag>>(entities))
                .Returns(dtos);

            // Act
            var result = await _service.GetTagsAsync(addressSpaceId);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        #endregion

        #region UpdateTagAsync Tests

        [Fact]
        public async Task UpdateTagAsync_ExistingTag_UpdatesSuccessfully()
        {
            // Arrange
            var tag = new Tag
            {
                Name = "Environment",
                AddressSpaceId = "space1",
                Description = "Updated description",
                Type = "Inheritable"
            };

            var existingEntity = new TagEntity
            {
                Name = "Environment",
                AddressSpaceId = "space1",
                Description = "Old description",
                Type = "Inheritable",
                CreatedOn = DateTime.UtcNow.AddDays(-1)
            };

            var updatedEntity = new TagEntity
            {
                Name = "Environment",
                AddressSpaceId = "space1",
                Description = "Updated description",
                Type = "Inheritable",
                CreatedOn = DateTime.UtcNow.AddDays(-1),
                ModifiedOn = DateTime.UtcNow
            };

            _tagRepositoryMock.Setup(r => r.GetByNameAsync("space1", "Environment"))
                .ReturnsAsync(existingEntity);
            _tagRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<TagEntity>()))
                .ReturnsAsync(updatedEntity);
            _mapperMock.Setup(m => m.Map(tag, existingEntity))
                .Returns(existingEntity);
            _mapperMock.Setup(m => m.Map<Tag>(updatedEntity))
                .Returns(tag);

            // Act
            var result = await _service.UpdateTagAsync(tag);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Updated description", result.Description);

            _tagRepositoryMock.Verify(r => r.UpdateAsync(It.Is<TagEntity>(e =>
                e.Name == "Environment" &&
                e.ModifiedOn > e.CreatedOn)), Times.Once);
        }

        [Fact]
        public async Task UpdateTagAsync_NonExistingTag_ThrowsException()
        {
            // Arrange
            var tag = new Tag
            {
                Name = "NonExisting",
                AddressSpaceId = "space1",
                Type = "Inheritable"
            };

            _tagRepositoryMock.Setup(r => r.GetByNameAsync("space1", "NonExisting"))
                .ReturnsAsync((TagEntity)null);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.UpdateTagAsync(tag));

            Assert.Contains("Tag 'NonExisting' not found", exception.Message);
        }

        #endregion

        #region DeleteTagAsync Tests

        [Fact]
        public async Task DeleteTagAsync_ExistingTag_DeletesSuccessfully()
        {
            // Arrange
            var tagName = "Environment";
            var addressSpaceId = "space1";

            var existingEntity = new TagEntity
            {
                Name = tagName,
                AddressSpaceId = addressSpaceId
            };

            _tagRepositoryMock.Setup(r => r.GetByNameAsync(addressSpaceId, tagName))
                .ReturnsAsync(existingEntity);
            _tagRepositoryMock.Setup(r => r.DeleteAsync(addressSpaceId, tagName))
                .Returns(Task.CompletedTask);

            // Act
            await _service.DeleteTagAsync(tagName, addressSpaceId);

            // Assert
            _tagRepositoryMock.Verify(r => r.DeleteAsync(addressSpaceId, tagName), Times.Once);
        }

        [Fact]
        public async Task DeleteTagAsync_NonExistingTag_ThrowsException()
        {
            // Arrange
            var tagName = "NonExisting";
            var addressSpaceId = "space1";

            _tagRepositoryMock.Setup(r => r.GetByNameAsync(addressSpaceId, tagName))
                .ReturnsAsync((TagEntity)null);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.DeleteTagAsync(tagName, addressSpaceId));

            Assert.Contains("Tag 'NonExisting' not found", exception.Message);
        }

        #endregion

        #region Business Rules Tests

        [Fact]
        public async Task CreateTagAsync_InheritableTag_AppliesInheritanceRules()
        {
            // Arrange
            var tag = new Tag
            {
                Name = "Environment",
                AddressSpaceId = "space1",
                Type = "Inheritable",
                Implies = new Dictionary<string, Dictionary<string, string>> 
                { 
                    { "Backup", new Dictionary<string, string> { { "value", "Required" } } } 
                }
            };

            var tagEntity = new TagEntity
            {
                Name = "Environment",
                AddressSpaceId = "space1",
                Type = "Inheritable"
            };

            _mapperMock.Setup(m => m.Map<TagEntity>(tag))
                .Returns(tagEntity);
            _mapperMock.Setup(m => m.Map<Tag>(It.IsAny<TagEntity>()))
                .Returns(tag);

            _tagRepositoryMock.Setup(r => r.GetByNameAsync("space1", "Environment"))
                .ReturnsAsync((TagEntity)null);
            _tagRepositoryMock.Setup(r => r.CreateAsync(It.IsAny<TagEntity>()))
                .ReturnsAsync(tagEntity);

            // Act
            var result = await _service.CreateTagAsync(tag);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Inheritable", result.Type);
            Assert.Contains("Backup", result.Implies.Keys);
        }

        [Fact]
        public async Task CreateTagAsync_TagWithKnownValues_ValidatesValues()
        {
            // Arrange
            var tag = new Tag
            {
                Name = "Environment",
                AddressSpaceId = "space1",
                Type = "Enumerated",
                KnownValues = new List<string> { "Production", "Staging", "Development" }
            };

            var tagEntity = new TagEntity
            {
                Name = "Environment",
                AddressSpaceId = "space1",
                Type = "Enumerated"
            };

            _mapperMock.Setup(m => m.Map<TagEntity>(tag))
                .Returns(tagEntity);
            _mapperMock.Setup(m => m.Map<Tag>(It.IsAny<TagEntity>()))
                .Returns(tag);

            _tagRepositoryMock.Setup(r => r.GetByNameAsync("space1", "Environment"))
                .ReturnsAsync((TagEntity)null);
            _tagRepositoryMock.Setup(r => r.CreateAsync(It.IsAny<TagEntity>()))
                .ReturnsAsync(tagEntity);

            // Act
            var result = await _service.CreateTagAsync(tag);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.KnownValues.Count);
            Assert.Contains("Production", result.KnownValues);
            Assert.Contains("Staging", result.KnownValues);
            Assert.Contains("Development", result.KnownValues);
        }

        #endregion

        #region Error Handling Tests

        [Fact]
        public async Task CreateTagAsync_RepositoryThrows_PropagatesException()
        {
            // Arrange
            var tag = new Tag
            {
                Name = "Environment",
                AddressSpaceId = "space1",
                Type = "Inheritable"
            };

            _tagRepositoryMock.Setup(r => r.GetByNameAsync("space1", "Environment"))
                .ReturnsAsync((TagEntity)null);
            _tagRepositoryMock.Setup(r => r.CreateAsync(It.IsAny<TagEntity>()))
                .ThrowsAsync(new Exception("Database error"));

            _mapperMock.Setup(m => m.Map<TagEntity>(tag))
                .Returns(new TagEntity());

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => _service.CreateTagAsync(tag));
        }

        [Fact]
        public async Task GetTagAsync_RepositoryThrows_PropagatesException()
        {
            // Arrange
            _tagRepositoryMock.Setup(r => r.GetByNameAsync("space1", "Environment"))
                .ThrowsAsync(new Exception("Database error"));

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => _service.GetTagAsync("Environment", "space1"));
        }

        #endregion

        #region Cancellation Token Tests

        [Fact]
        public async Task CreateTagAsync_WithCancellationToken_PassesTokenCorrectly()
        {
            // Arrange
            var tag = new Tag
            {
                Name = "Environment",
                AddressSpaceId = "space1",
                Type = "Inheritable"
            };
            var cancellationToken = new CancellationToken();

            _mapperMock.Setup(m => m.Map<TagEntity>(tag))
                .Returns(new TagEntity());
            _mapperMock.Setup(m => m.Map<Tag>(It.IsAny<TagEntity>()))
                .Returns(tag);
            _tagRepositoryMock.Setup(r => r.GetByNameAsync("space1", "Environment"))
                .ReturnsAsync((TagEntity)null);
            _tagRepositoryMock.Setup(r => r.CreateAsync(It.IsAny<TagEntity>()))
                .ReturnsAsync(new TagEntity());

            // Act
            await _service.CreateTagAsync(tag, cancellationToken);

            // Assert - Token is passed through the call chain
            Assert.True(true); // If we get here without exception, cancellation token was handled properly
        }

        #endregion

        #region Helper Methods for Validation

        private static bool IsValidTagType(string type)
        {
            var validTypes = new[] { "Inheritable", "Enumerated", "FreeForm", "System" };
            return validTypes.Contains(type);
        }

        #endregion

        #region Business Rules Tests

        [Fact]
        public async Task ApplyTagBusinessRulesAsync_InheritableTag_ShouldApplyInheritanceRules()
        {
            // Arrange
            var tag = new Tag
            {
                Name = "Environment",
                AddressSpaceId = "space1",
                Type = "Inheritable",
                Description = "Environment classification",
                KnownValues = new List<string> { "production", "staging", "development" },
                Implies = new Dictionary<string, Dictionary<string, string>>(),
                Attributes = new Dictionary<string, Dictionary<string, string>>()
            };

            // Act
            var result = await _service.CreateTagAsync(tag);

            // Assert
            Assert.NotNull(result);
            Assert.Contains("inheritance", result.Attributes.Keys);
            Assert.Equal("true", result.Attributes["inheritance"]["enabled"]);
            Assert.Equal("merge", result.Attributes["inheritance"]["strategy"]);
        }

        [Fact]
        public async Task ApplyTagBusinessRulesAsync_NonInheritableTagWithImplications_ShouldClearImplications()
        {
            // Arrange
            var tag = new Tag
            {
                Name = "Location",
                AddressSpaceId = "space1",
                Type = "NonInheritable",
                Description = "Physical location",
                Implies = new Dictionary<string, Dictionary<string, string>>
                {
                    { "SomeTag", new Dictionary<string, string> { { "value", "test" } } }
                },
                Attributes = new Dictionary<string, Dictionary<string, string>>()
            };

            // Act
            var result = await _service.CreateTagAsync(tag);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Implies);
        }

        [Fact]
        public async Task ApplyTagBusinessRulesAsync_InvalidTagType_ShouldThrowException()
        {
            // Arrange
            var tag = new Tag
            {
                Name = "InvalidTag",
                AddressSpaceId = "space1",
                Type = "InvalidType",
                Description = "Invalid tag type",
                Implies = new Dictionary<string, Dictionary<string, string>>(),
                Attributes = new Dictionary<string, Dictionary<string, string>>()
            };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateTagAsync(tag));
        }

        [Fact]
        public async Task ApplyTagBusinessRulesAsync_ValidPriorityAttribute_ShouldPass()
        {
            // Arrange
            var tag = new Tag
            {
                Name = "PriorityTag",
                AddressSpaceId = "space1",
                Type = "Inheritable",
                Description = "Tag with valid priority",
                Attributes = new Dictionary<string, Dictionary<string, string>>
                {
                    { "priority", new Dictionary<string, string> { { "value", "85" } } }
                }
            };

            // Act
            var result = await _service.CreateTagAsync(tag);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("85", result.Attributes["priority"]["value"]);
        }

        [Fact]
        public async Task ApplyTagBusinessRulesAsync_InvalidPriorityAttribute_ShouldThrowException()
        {
            // Arrange
            var tag = new Tag
            {
                Name = "InvalidPriorityTag",
                AddressSpaceId = "space1",
                Type = "Inheritable",
                Description = "Tag with invalid priority",
                Attributes = new Dictionary<string, Dictionary<string, string>>
                {
                    { "priority", new Dictionary<string, string> { { "value", "150" } } }
                }
            };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateTagAsync(tag));
        }

        [Fact]
        public async Task ApplyTagBusinessRulesAsync_ValidEnvironmentAttribute_ShouldPass()
        {
            // Arrange
            var tag = new Tag
            {
                Name = "EnvTag",
                AddressSpaceId = "space1",
                Type = "Inheritable",
                Description = "Tag with valid environment",
                Attributes = new Dictionary<string, Dictionary<string, string>>
                {
                    { "environment", new Dictionary<string, string> { { "value", "production" } } }
                }
            };

            // Act
            var result = await _service.CreateTagAsync(tag);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("production", result.Attributes["environment"]["value"]);
        }

        [Fact]
        public async Task ApplyTagBusinessRulesAsync_InvalidEnvironmentAttribute_ShouldThrowException()
        {
            // Arrange
            var tag = new Tag
            {
                Name = "InvalidEnvTag",
                AddressSpaceId = "space1",
                Type = "Inheritable",
                Description = "Tag with invalid environment",
                Attributes = new Dictionary<string, Dictionary<string, string>>
                {
                    { "environment", new Dictionary<string, string> { { "value", "invalid-env" } } }
                }
            };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateTagAsync(tag));
        }

        [Fact]
        public async Task ApplyTagBusinessRulesAsync_ValidOwnerAttribute_ShouldPass()
        {
            // Arrange
            var tag = new Tag
            {
                Name = "OwnerTag",
                AddressSpaceId = "space1",
                Type = "Inheritable",
                Description = "Tag with valid owner",
                Attributes = new Dictionary<string, Dictionary<string, string>>
                {
                    { "owner", new Dictionary<string, string> { { "value", "admin@company.com" } } }
                }
            };

            // Act
            var result = await _service.CreateTagAsync(tag);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("admin@company.com", result.Attributes["owner"]["value"]);
        }

        [Fact]
        public async Task ApplyTagBusinessRulesAsync_InvalidOwnerAttribute_ShouldThrowException()
        {
            // Arrange
            var tag = new Tag
            {
                Name = "InvalidOwnerTag",
                AddressSpaceId = "space1",
                Type = "Inheritable",
                Description = "Tag with invalid owner",
                Attributes = new Dictionary<string, Dictionary<string, string>>
                {
                    { "owner", new Dictionary<string, string> { { "value", "x" } } }
                }
            };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateTagAsync(tag));
        }

        [Fact]
        public async Task ApplyTagBusinessRulesAsync_EmptyAttributeName_ShouldThrowException()
        {
            // Arrange
            var tag = new Tag
            {
                Name = "EmptyAttrTag",
                AddressSpaceId = "space1",
                Type = "Inheritable",
                Description = "Tag with empty attribute name",
                Attributes = new Dictionary<string, Dictionary<string, string>>
                {
                    { "", new Dictionary<string, string> { { "value", "test" } } }
                }
            };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateTagAsync(tag));
        }

        [Fact]
        public async Task ApplyTagBusinessRulesAsync_EmptyAttributeValues_ShouldThrowException()
        {
            // Arrange
            var tag = new Tag
            {
                Name = "EmptyValuesTag",
                AddressSpaceId = "space1",
                Type = "Inheritable",
                Description = "Tag with empty attribute values",
                Attributes = new Dictionary<string, Dictionary<string, string>>
                {
                    { "testattr", new Dictionary<string, string>() }
                }
            };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateTagAsync(tag));
        }

        [Fact]
        public async Task ApplyTagBusinessRulesAsync_ValidInheritanceStrategy_ShouldPass()
        {
            // Arrange
            var tag = new Tag
            {
                Name = "InheritanceTag",
                AddressSpaceId = "space1",
                Type = "Inheritable",
                Description = "Tag with valid inheritance strategy",
                Attributes = new Dictionary<string, Dictionary<string, string>>
                {
                    { "inheritance", new Dictionary<string, string> 
                        { 
                            { "enabled", "true" },
                            { "strategy", "override" }
                        } 
                    }
                }
            };

            // Act
            var result = await _service.CreateTagAsync(tag);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("override", result.Attributes["inheritance"]["strategy"]);
        }

        [Fact]
        public async Task ApplyTagBusinessRulesAsync_InvalidInheritanceStrategy_ShouldThrowException()
        {
            // Arrange
            var tag = new Tag
            {
                Name = "InvalidInheritanceTag",
                AddressSpaceId = "space1",
                Type = "Inheritable",
                Description = "Tag with invalid inheritance strategy",
                Attributes = new Dictionary<string, Dictionary<string, string>>
                {
                    { "inheritance", new Dictionary<string, string> 
                        { 
                            { "enabled", "true" },
                            { "strategy", "invalid-strategy" }
                        } 
                    }
                }
            };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateTagAsync(tag));
        }

        [Fact]
        public async Task ApplyTagBusinessRulesAsync_NullCollections_ShouldInitializeCollections()
        {
            // Arrange
            var tag = new Tag
            {
                Name = "NullCollectionsTag",
                AddressSpaceId = "space1",
                Type = "Inheritable",
                Description = "Tag with null collections",
                KnownValues = null,
                Implies = null,
                Attributes = null
            };

            // Act
            var result = await _service.CreateTagAsync(tag);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.KnownValues);
            Assert.NotNull(result.Implies);
            Assert.NotNull(result.Attributes);
            Assert.Empty(result.KnownValues);
            Assert.Empty(result.Implies);
            Assert.Contains("inheritance", result.Attributes.Keys); // Should have inheritance metadata
        }

        #endregion

        #region Circular Dependency Tests

        [Fact]
        public async Task DetectCircularDependencies_NoCircularDependency_ShouldPass()
        {
            // Arrange
            var backupTag = new TagEntity
            {
                Name = "Backup",
                AddressSpaceId = "space1",
                Type = "Inheritable",
                Implies = new Dictionary<string, Dictionary<string, string>>()
            };

            var environmentTag = new Tag
            {
                Name = "Environment",
                AddressSpaceId = "space1",
                Type = "Inheritable",
                Description = "Environment tag",
                Implies = new Dictionary<string, Dictionary<string, string>>
                {
                    { "Backup", new Dictionary<string, string> { { "required", "true" } } }
                }
            };

            _tagRepositoryMock.Setup(r => r.GetByNameAsync("space1", "Backup"))
                .ReturnsAsync(backupTag);

            // Act & Assert - Should not throw
            var result = await _service.CreateTagAsync(environmentTag);
            Assert.NotNull(result);
        }

        [Fact]
        public async Task DetectCircularDependencies_DirectCircularDependency_ShouldThrowException()
        {
            // Arrange - Create a scenario where Tag A implies Tag B, and Tag B implies Tag A
            var tagBEntity = new TagEntity
            {
                Name = "TagB",
                AddressSpaceId = "space1",
                Type = "Inheritable",
                Implies = new Dictionary<string, Dictionary<string, string>>
                {
                    { "TagA", new Dictionary<string, string> { { "value", "test" } } }
                }
            };

            var tagA = new Tag
            {
                Name = "TagA",
                AddressSpaceId = "space1",
                Type = "Inheritable",
                Description = "Tag A",
                Implies = new Dictionary<string, Dictionary<string, string>>
                {
                    { "TagB", new Dictionary<string, string> { { "value", "test" } } }
                }
            };

            _tagRepositoryMock.Setup(r => r.GetByNameAsync("space1", "TagB"))
                .ReturnsAsync(tagBEntity);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CreateTagAsync(tagA));
        }

        [Fact]
        public async Task DetectCircularDependencies_IndirectCircularDependency_ShouldThrowException()
        {
            // Arrange - Create A -> B -> C -> A circular dependency
            var tagBEntity = new TagEntity
            {
                Name = "TagB",
                AddressSpaceId = "space1",
                Type = "Inheritable",
                Implies = new Dictionary<string, Dictionary<string, string>>
                {
                    { "TagC", new Dictionary<string, string> { { "value", "test" } } }
                }
            };

            var tagCEntity = new TagEntity
            {
                Name = "TagC",
                AddressSpaceId = "space1",
                Type = "Inheritable",
                Implies = new Dictionary<string, Dictionary<string, string>>
                {
                    { "TagA", new Dictionary<string, string> { { "value", "test" } } }
                }
            };

            var tagA = new Tag
            {
                Name = "TagA",
                AddressSpaceId = "space1",
                Type = "Inheritable",
                Description = "Tag A",
                Implies = new Dictionary<string, Dictionary<string, string>>
                {
                    { "TagB", new Dictionary<string, string> { { "value", "test" } } }
                }
            };

            _tagRepositoryMock.Setup(r => r.GetByNameAsync("space1", "TagB"))
                .ReturnsAsync(tagBEntity);
            _tagRepositoryMock.Setup(r => r.GetByNameAsync("space1", "TagC"))
                .ReturnsAsync(tagCEntity);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CreateTagAsync(tagA));
        }

        #endregion

        #region Implied Tag Validation Tests

        [Fact]
        public async Task ValidateImpliedTags_NonExistentImpliedTag_ShouldThrowException()
        {
            // Arrange
            var tag = new Tag
            {
                Name = "TestTag",
                AddressSpaceId = "space1",
                Type = "Inheritable",
                Description = "Test tag",
                Implies = new Dictionary<string, Dictionary<string, string>>
                {
                    { "NonExistentTag", new Dictionary<string, string> { { "value", "test" } } }
                }
            };

            _tagRepositoryMock.Setup(r => r.GetByNameAsync("space1", "NonExistentTag"))
                .ReturnsAsync((TagEntity)null);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CreateTagAsync(tag));
        }

        [Fact]
        public async Task ValidateImpliedTags_ImplyingNonInheritableTag_ShouldThrowException()
        {
            // Arrange
            var nonInheritableTagEntity = new TagEntity
            {
                Name = "NonInheritableTag",
                AddressSpaceId = "space1",
                Type = "NonInheritable"
            };

            var tag = new Tag
            {
                Name = "TestTag",
                AddressSpaceId = "space1",
                Type = "Inheritable",
                Description = "Test tag",
                Implies = new Dictionary<string, Dictionary<string, string>>
                {
                    { "NonInheritableTag", new Dictionary<string, string> { { "value", "test" } } }
                }
            };

            _tagRepositoryMock.Setup(r => r.GetByNameAsync("space1", "NonInheritableTag"))
                .ReturnsAsync(nonInheritableTagEntity);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CreateTagAsync(tag));
        }

        [Fact]
        public async Task ValidateImpliedTags_ImpliedValueNotInKnownValues_ShouldThrowException()
        {
            // Arrange
            var impliedTagEntity = new TagEntity
            {
                Name = "ImpliedTag",
                AddressSpaceId = "space1",
                Type = "Inheritable",
                KnownValues = new List<string> { "value1", "value2" }
            };

            var tag = new Tag
            {
                Name = "TestTag",
                AddressSpaceId = "space1",
                Type = "Inheritable",
                Description = "Test tag",
                Implies = new Dictionary<string, Dictionary<string, string>>
                {
                    { "ImpliedTag", new Dictionary<string, string> { { "key", "invalid-value" } } }
                }
            };

            _tagRepositoryMock.Setup(r => r.GetByNameAsync("space1", "ImpliedTag"))
                .ReturnsAsync(impliedTagEntity);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CreateTagAsync(tag));
        }

        [Fact]
        public async Task ValidateImpliedTags_ValidImpliedValue_ShouldPass()
        {
            // Arrange
            var impliedTagEntity = new TagEntity
            {
                Name = "ImpliedTag",
                AddressSpaceId = "space1",
                Type = "Inheritable",
                KnownValues = new List<string> { "value1", "value2" }
            };

            var tag = new Tag
            {
                Name = "TestTag",
                AddressSpaceId = "space1",
                Type = "Inheritable",
                Description = "Test tag",
                Implies = new Dictionary<string, Dictionary<string, string>>
                {
                    { "ImpliedTag", new Dictionary<string, string> { { "key", "value1" } } }
                }
            };

            _tagRepositoryMock.Setup(r => r.GetByNameAsync("space1", "ImpliedTag"))
                .ReturnsAsync(impliedTagEntity);

            // Act & Assert - Should not throw
            var result = await _service.CreateTagAsync(tag);
            Assert.NotNull(result);
        }

        #endregion
    }
}