using AutoMapper;
using Ipam.ServiceContract.DTOs;
using Ipam.DataAccess.Entities;
using System.Collections.Generic;
using System.Linq;

namespace Ipam.DataAccess.Extensions
{
    /// <summary>
    /// Extension methods for entity-DTO mapping
    /// </summary>
    /// <remarks>
    /// Author: IPAM Team
    /// Date: 2024-01-20
    /// </remarks>
    public static class MappingExtensions
    {
        /// <summary>
        /// Maps AddressSpaceEntity to AddressSpace DTO
        /// </summary>
        public static AddressSpace ToDto(this AddressSpaceEntity entity, IMapper mapper)
        {
            return mapper.Map<AddressSpace>(entity);
        }

        /// <summary>
        /// Maps AddressSpace DTO to AddressSpaceEntity
        /// </summary>
        public static AddressSpaceEntity ToEntity(this AddressSpace dto, IMapper mapper)
        {
            return mapper.Map<AddressSpaceEntity>(dto);
        }

        /// <summary>
        /// Maps IpAllocationEntity to IpAllocation DTO
        /// </summary>
        public static IpAllocation ToDto(this IpAllocationEntity entity, IMapper mapper)
        {
            return mapper.Map<IpAllocation>(entity);
        }

        /// <summary>
        /// Maps IpAllocation DTO to IpAllocationEntity
        /// </summary>
        public static IpAllocationEntity ToEntity(this IpAllocation dto, IMapper mapper)
        {
            return mapper.Map<IpAllocationEntity>(dto);
        }

        /// <summary>
        /// Maps TagEntity to Tag DTO
        /// </summary>
        public static Tag ToDto(this TagEntity entity, IMapper mapper)
        {
            return mapper.Map<Tag>(entity);
        }

        /// <summary>
        /// Maps Tag DTO to TagEntity
        /// </summary>
        public static TagEntity ToEntity(this Tag dto, IMapper mapper)
        {
            return mapper.Map<TagEntity>(dto);
        }

        /// <summary>
        /// Maps collection of AddressSpaceEntity to AddressSpace DTOs
        /// </summary>
        public static IEnumerable<AddressSpace> ToDtos(this IEnumerable<AddressSpaceEntity> entities, IMapper mapper)
        {
            return entities.Select(entity => entity.ToDto(mapper));
        }

        /// <summary>
        /// Maps collection of IpAllocationEntity to IpAllocation DTOs
        /// </summary>
        public static IEnumerable<IpAllocation> ToDtos(this IEnumerable<IpAllocationEntity> entities, IMapper mapper)
        {
            return entities.Select(entity => entity.ToDto(mapper));
        }

        /// <summary>
        /// Maps collection of TagEntity to Tag DTOs
        /// </summary>
        public static IEnumerable<Tag> ToDtos(this IEnumerable<TagEntity> entities, IMapper mapper)
        {
            return entities.Select(entity => entity.ToDto(mapper));
        }
    }
}