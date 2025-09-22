using Ipam.DataAccess.Interfaces;
using Ipam.DataAccess.Entities;
using Ipam.ServiceContract.DTOs;
using Ipam.ServiceContract.Interfaces;
using AutoMapper;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ipam.DataAccess.Services
{
    /// <summary>
    /// Implementation of tag service with inheritance and validation
    /// </summary>
    /// <remarks>
    /// Author: IPAM Team
    /// Date: 2024-01-20
    /// </remarks>
    public class TagServiceImpl : ITagService
    {
        private readonly ITagRepository _tagRepository;
        private readonly TagInheritanceService _tagInheritanceService;
        private readonly IMapper _mapper;
        private readonly ILogger<TagServiceImpl> _logger;

        public TagServiceImpl(
            ITagRepository tagRepository,
            TagInheritanceService tagInheritanceService,
            IMapper mapper,
            ILogger<TagServiceImpl> logger)
        {
            _tagRepository = tagRepository;
            _tagInheritanceService = tagInheritanceService;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<Tag> CreateTagAsync(Tag tag, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Creating tag {TagName} in address space {AddressSpaceId}", tag.Name, tag.AddressSpaceId);

                // Validate the tag object
                await ValidateTagAsync(tag);

                // Apply business rules
                await ApplyTagBusinessRulesAsync(tag);

                // Set system properties
                tag.CreatedOn = DateTime.UtcNow;
                tag.ModifiedOn = DateTime.UtcNow;

                // Map to entity and create
                var entity = _mapper.Map<TagEntity>(tag);
                var createdEntity = await _tagRepository.CreateAsync(entity);
                var result = _mapper.Map<Tag>(createdEntity);
                
                _logger.LogInformation("Successfully created tag {TagName}", tag.Name);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create tag {TagName} in address space {AddressSpaceId}", tag?.Name, tag?.AddressSpaceId);
                throw;
            }
        }

        public async Task<Tag?> GetTagAsync(string name, string addressSpaceId, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogDebug("Getting tag {TagName} from address space {AddressSpaceId}", name, addressSpaceId);
                
                var entity = await _tagRepository.GetByNameAsync(addressSpaceId, name);
                if (entity == null)
                {
                    _logger.LogWarning("Tag {TagName} not found in address space {AddressSpaceId}", name, addressSpaceId);
                    return null;
                }

                return _mapper.Map<Tag>(entity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get tag {TagName} from address space {AddressSpaceId}", name, addressSpaceId);
                throw;
            }
        }

        public async Task<IEnumerable<Tag>> GetTagsAsync(string addressSpaceId, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogDebug("Getting all tags from address space {AddressSpaceId}", addressSpaceId);
                
                var entities = await _tagRepository.GetAllAsync(addressSpaceId);
                var result = _mapper.Map<IEnumerable<Tag>>(entities);
                
                _logger.LogDebug("Retrieved {Count} tags from address space {AddressSpaceId}", 
                    result.Count(), addressSpaceId);
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get tags from address space {AddressSpaceId}", addressSpaceId);
                throw;
            }
        }

        public async Task<Tag> UpdateTagAsync(Tag tag, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Updating tag {TagName} in address space {AddressSpaceId}", tag.Name, tag.AddressSpaceId);

                // Validate the tag object
                await ValidateTagAsync(tag);

                var entity = await _tagRepository.GetByNameAsync(tag.AddressSpaceId, tag.Name);
                if (entity == null)
                {
                    _logger.LogWarning("Tag {TagName} not found for update in address space {AddressSpaceId}", 
                        tag.Name, tag.AddressSpaceId);
                    return null;
                }

                // Apply business rules
                await ApplyTagBusinessRulesAsync(tag);

                // Map updated values while preserving entity metadata
                _mapper.Map(tag, entity);
                entity.ModifiedOn = DateTime.UtcNow;

                var updatedEntity = await _tagRepository.UpdateAsync(entity);
                var result = _mapper.Map<Tag>(updatedEntity);
                
                _logger.LogInformation("Successfully updated tag {TagName}", tag.Name);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update tag {TagName} in address space {AddressSpaceId}", 
                    tag?.Name, tag?.AddressSpaceId);
                throw;
            }
        }

        public async Task DeleteTagAsync(string name, string addressSpaceId, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Deleting tag {TagName} from address space {AddressSpaceId}", name, addressSpaceId);
                
                await _tagRepository.DeleteAsync(addressSpaceId, name);
                
                _logger.LogInformation("Successfully deleted tag {TagName}", name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete tag {TagName} from address space {AddressSpaceId}", 
                    name, addressSpaceId);
                throw;
            }
        }

        public async Task<Dictionary<string, string>> ApplyTagImplicationsAsync(string addressSpaceId, Dictionary<string, string> inputTags)
        {
            try
            {
                _logger.LogDebug("Applying tag implications for {TagCount} tags in address space {AddressSpaceId}", 
                    inputTags?.Count ?? 0, addressSpaceId);
                
                var result = await _tagInheritanceService.ApplyTagImplications(addressSpaceId, inputTags);
                
                _logger.LogDebug("Applied tag implications, resulting in {ResultTagCount} tags", result.Count);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to apply tag implications in address space {AddressSpaceId}", addressSpaceId);
                throw;
            }
        }

        public async Task ValidateTagInheritanceAsync(string addressSpaceId, Dictionary<string, string> parentTags, Dictionary<string, string> childTags)
        {
            try
            {
                _logger.LogDebug("Validating tag inheritance in address space {AddressSpaceId}", addressSpaceId);
                
                await _tagInheritanceService.ValidateTagInheritance(addressSpaceId, parentTags, childTags);
                
                _logger.LogDebug("Tag inheritance validation passed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Tag inheritance validation failed in address space {AddressSpaceId}", addressSpaceId);
                throw;
            }
        }

        /// <summary>
        /// Validates a tag object for business rules
        /// </summary>
        private async Task ValidateTagAsync(Tag tag)
        {
            if (tag == null)
                throw new ArgumentNullException(nameof(tag));

            if (string.IsNullOrWhiteSpace(tag.AddressSpaceId))
                throw new ArgumentException("AddressSpaceId is required", nameof(tag));

            if (string.IsNullOrWhiteSpace(tag.Name))
                throw new ArgumentException("Tag name is required", nameof(tag));

            if (string.IsNullOrWhiteSpace(tag.Type))
                throw new ArgumentException("Tag type is required", nameof(tag));

            if (tag.Type != "Inheritable" && tag.Type != "NonInheritable")
                throw new ArgumentException("Tag type must be 'Inheritable' or 'NonInheritable'", nameof(tag));

            // Validate known values if specified
            if (tag.KnownValues != null && tag.KnownValues.Count > 0)
            {
                var duplicates = tag.KnownValues.GroupBy(x => x).Where(g => g.Count() > 1).Select(g => g.Key);
                if (duplicates.Any())
                    throw new ArgumentException($"Duplicate known values found: {string.Join(", ", duplicates)}", nameof(tag));
            }

            // TODO: Add validation for circular dependencies in implies
            // TODO: Add validation for tag conflicts with existing tags
        }

        /// <summary>
        /// Applies business rules to a tag
        /// </summary>
        private async Task ApplyTagBusinessRulesAsync(Tag tag)
        {
            _logger.LogDebug("Applying business rules for tag {TagName} of type {TagType}", tag.Name, tag.Type);

            // Ensure collections are not null
            tag.KnownValues ??= new List<string>();
            tag.Implies ??= new Dictionary<string, Dictionary<string, string>>();
            tag.Attributes ??= new Dictionary<string, Dictionary<string, string>>();

            // Apply type-specific business rules
            await ApplyTypeSpecificRulesAsync(tag);

            // Validate and process implications
            if (tag.Implies.Any())
            {
                await ValidateImpliedTagsAsync(tag);
                await DetectCircularDependenciesAsync(tag);
            }

            // Apply attribute validation rules
            await ValidateTagAttributesAsync(tag);

            // Apply inheritance rules for inheritable tags
            if (tag.Type == "Inheritable")
            {
                await ApplyInheritanceRulesAsync(tag);
            }

            _logger.LogDebug("Successfully applied business rules for tag {TagName}", tag.Name);
        }

        /// <summary>
        /// Applies type-specific business rules
        /// </summary>
        private async Task ApplyTypeSpecificRulesAsync(Tag tag)
        {
            switch (tag.Type)
            {
                case "Inheritable":
                    // Inheritable tags can have implications and should be validated for inheritance chains
                    if (tag.Implies.Any())
                    {
                        _logger.LogDebug("Processing {ImplicationCount} implications for inheritable tag {TagName}", 
                            tag.Implies.Count, tag.Name);
                    }
                    break;

                case "NonInheritable":
                    // Non-inheritable tags should not have implications
                    if (tag.Implies.Any())
                    {
                        _logger.LogWarning("Non-inheritable tag {TagName} has implications, which will be ignored", tag.Name);
                        tag.Implies.Clear();
                    }
                    break;

                default:
                    throw new ArgumentException($"Invalid tag type: {tag.Type}. Must be 'Inheritable' or 'NonInheritable'");
            }

            await Task.CompletedTask;
        }

        /// <summary>
        /// Validates that all implied tags exist and are compatible
        /// </summary>
        private async Task ValidateImpliedTagsAsync(Tag tag)
        {
            var invalidImplications = new List<string>();

            foreach (var implication in tag.Implies)
            {
                var impliedTagName = implication.Key;
                var impliedTagValue = implication.Value;

                try
                {
                    // Check if the implied tag exists
                    var impliedTag = await _tagRepository.GetByNameAsync(tag.AddressSpaceId, impliedTagName);
                    if (impliedTag == null)
                    {
                        invalidImplications.Add($"Implied tag '{impliedTagName}' does not exist");
                        continue;
                    }

                    // Validate compatibility
                    await ValidateTagCompatibilityAsync(tag, _mapper.Map<Tag>(impliedTag), impliedTagValue);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error validating implied tag {ImpliedTagName} for tag {TagName}", 
                        impliedTagName, tag.Name);
                    invalidImplications.Add($"Error validating implied tag '{impliedTagName}': {ex.Message}");
                }
            }

            if (invalidImplications.Any())
            {
                var errorMessage = $"Invalid tag implications found: {string.Join("; ", invalidImplications)}";
                throw new InvalidOperationException(errorMessage);
            }
        }

        /// <summary>
        /// Validates compatibility between two tags
        /// </summary>
        private async Task ValidateTagCompatibilityAsync(Tag sourceTag, Tag impliedTag, Dictionary<string, string> impliedValues)
        {
            // Check if implied tag is inheritable (only inheritable tags can be implied)
            if (impliedTag.Type != "Inheritable")
            {
                throw new InvalidOperationException($"Cannot imply non-inheritable tag '{impliedTag.Name}'");
            }

            // Validate that implied values are within known values if specified
            if (impliedTag.KnownValues != null && impliedTag.KnownValues.Any())
            {
                foreach (var impliedValue in impliedValues.Values)
                {
                    if (!impliedTag.KnownValues.Contains(impliedValue))
                    {
                        throw new InvalidOperationException(
                            $"Implied value '{impliedValue}' for tag '{impliedTag.Name}' is not in known values: {string.Join(", ", impliedTag.KnownValues)}");
                    }
                }
            }

            await Task.CompletedTask;
        }

        /// <summary>
        /// Detects circular dependencies in tag implications
        /// </summary>
        private async Task DetectCircularDependenciesAsync(Tag tag)
        {
            var visited = new HashSet<string>();
            var recursionStack = new HashSet<string>();

            await DetectCircularDependencyRecursive(tag.Name, visited, recursionStack, tag.AddressSpaceId, tag.Implies);
        }

        /// <summary>
        /// Recursively detects circular dependencies
        /// </summary>
        private async Task DetectCircularDependencyRecursive(
            string currentTagName, 
            HashSet<string> visited, 
            HashSet<string> recursionStack, 
            string addressSpaceId,
            Dictionary<string, Dictionary<string, string>> currentImplications = null)
        {
            if (recursionStack.Contains(currentTagName))
            {
                throw new InvalidOperationException($"Circular dependency detected in tag implications involving '{currentTagName}'");
            }

            if (visited.Contains(currentTagName))
            {
                return; // Already processed this tag
            }

            visited.Add(currentTagName);
            recursionStack.Add(currentTagName);

            try
            {
                // Get implications for current tag
                Dictionary<string, Dictionary<string, string>> implications;
                
                if (currentImplications != null)
                {
                    implications = currentImplications;
                }
                else
                {
                    var currentTag = await _tagRepository.GetByNameAsync(addressSpaceId, currentTagName);
                    if (currentTag == null)
                    {
                        return; // Tag doesn't exist, skip
                    }
                    
                    var mappedTag = _mapper.Map<Tag>(currentTag);
                    implications = mappedTag.Implies ?? new Dictionary<string, Dictionary<string, string>>();
                }

                // Recursively check each implied tag
                foreach (var impliedTagName in implications.Keys)
                {
                    await DetectCircularDependencyRecursive(impliedTagName, visited, recursionStack, addressSpaceId);
                }
            }
            finally
            {
                recursionStack.Remove(currentTagName);
            }
        }

        /// <summary>
        /// Validates tag attributes according to business rules
        /// </summary>
        private async Task ValidateTagAttributesAsync(Tag tag)
        {
            if (tag.Attributes == null || !tag.Attributes.Any())
            {
                return; // No attributes to validate
            }

            var invalidAttributes = new List<string>();

            foreach (var attribute in tag.Attributes)
            {
                var attributeName = attribute.Key;
                var attributeValues = attribute.Value;

                // Validate attribute name
                if (string.IsNullOrWhiteSpace(attributeName))
                {
                    invalidAttributes.Add("Attribute name cannot be empty");
                    continue;
                }

                // Validate attribute values
                if (attributeValues == null || !attributeValues.Any())
                {
                    invalidAttributes.Add($"Attribute '{attributeName}' must have at least one value");
                    continue;
                }

                // Apply attribute-specific validation rules
                await ValidateSpecificAttributeAsync(attributeName, attributeValues, invalidAttributes);
            }

            if (invalidAttributes.Any())
            {
                var errorMessage = $"Invalid tag attributes: {string.Join("; ", invalidAttributes)}";
                throw new ArgumentException(errorMessage);
            }
        }

        /// <summary>
        /// Validates specific attribute types with custom rules
        /// </summary>
        private async Task ValidateSpecificAttributeAsync(string attributeName, Dictionary<string, string> attributeValues, List<string> invalidAttributes)
        {
            switch (attributeName.ToLowerInvariant())
            {
                case "priority":
                    // Priority should be a valid integer
                    foreach (var value in attributeValues.Values)
                    {
                        if (!int.TryParse(value, out var priority) || priority < 0 || priority > 100)
                        {
                            invalidAttributes.Add($"Priority value '{value}' must be an integer between 0 and 100");
                        }
                    }
                    break;

                case "environment":
                    // Environment should be from predefined list
                    var validEnvironments = new[] { "development", "staging", "production", "test" };
                    foreach (var value in attributeValues.Values)
                    {
                        if (!validEnvironments.Contains(value.ToLowerInvariant()))
                        {
                            invalidAttributes.Add($"Environment value '{value}' must be one of: {string.Join(", ", validEnvironments)}");
                        }
                    }
                    break;

                case "owner":
                    // Owner should be a valid email or username format
                    foreach (var value in attributeValues.Values)
                    {
                        if (string.IsNullOrWhiteSpace(value) || (!value.Contains("@") && value.Length < 3))
                        {
                            invalidAttributes.Add($"Owner value '{value}' should be a valid email or username");
                        }
                    }
                    break;

                // Add more attribute-specific validations as needed
            }

            await Task.CompletedTask;
        }

        /// <summary>
        /// Applies inheritance-specific business rules
        /// </summary>
        private async Task ApplyInheritanceRulesAsync(Tag tag)
        {
            // Ensure inheritable tags have proper structure for inheritance
            if (tag.Type == "Inheritable")
            {
                // Add default inheritance metadata if not present
                if (!tag.Attributes.ContainsKey("inheritance"))
                {
                    tag.Attributes["inheritance"] = new Dictionary<string, string>
                    {
                        { "enabled", "true" },
                        { "strategy", "merge" }
                    };
                }

                // Validate inheritance strategy
                if (tag.Attributes.ContainsKey("inheritance"))
                {
                    var inheritanceConfig = tag.Attributes["inheritance"];
                    if (inheritanceConfig.ContainsKey("strategy"))
                    {
                        var strategy = inheritanceConfig["strategy"];
                        var validStrategies = new[] { "merge", "override", "append" };
                        if (!validStrategies.Contains(strategy.ToLowerInvariant()))
                        {
                            throw new ArgumentException($"Invalid inheritance strategy '{strategy}'. Must be one of: {string.Join(", ", validStrategies)}");
                        }
                    }
                }

                _logger.LogDebug("Applied inheritance rules for tag {TagName}", tag.Name);
            }

            await Task.CompletedTask;
        }
    }
}