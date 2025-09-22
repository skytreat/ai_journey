using AutoMapper;
using Ipam.ServiceContract.DTOs;
using Ipam.DataAccess.Entities;
using System.Linq;

namespace Ipam.DataAccess.Mapping
{
    /// <summary>
    /// AutoMapper profile for mapping between entities and DTOs
    /// </summary>
    /// <remarks>
    /// Author: IPAM Team
    /// Date: 2024-01-20
    /// </remarks>
    public class EntityDtoMappingProfile : Profile
    {
        public EntityDtoMappingProfile()
        {
            CreateEntityToDtoMaps();
            CreateDtoToEntityMaps();
        }

        private void CreateEntityToDtoMaps()
        {
            // AddressSpaceEntity -> AddressSpace
            CreateMap<AddressSpaceEntity, AddressSpace>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
                .ForMember(dest => dest.CreatedOn, opt => opt.MapFrom(src => src.CreatedOn))
                .ForMember(dest => dest.ModifiedOn, opt => opt.MapFrom(src => src.ModifiedOn))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status));

            // IpAllocationEntity -> IpAllocation
            CreateMap<IpAllocationEntity, IpAllocation>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.AddressSpaceId, opt => opt.MapFrom(src => src.AddressSpaceId))
                .ForMember(dest => dest.Prefix, opt => opt.MapFrom(src => src.Prefix))
                .ForMember(dest => dest.ParentId, opt => opt.MapFrom(src => src.ParentId))
                .ForMember(dest => dest.ChildrenIds, opt => opt.MapFrom(src => src.ChildrenIds))
                .ForMember(dest => dest.CreatedOn, opt => opt.MapFrom(src => src.CreatedOn))
                .ForMember(dest => dest.ModifiedOn, opt => opt.MapFrom(src => src.ModifiedOn))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => "Active")) // Default status for DTO
                .ForMember(dest => dest.Tags, opt => opt.MapFrom(src => src.Tags));

            // TagEntity -> Tag
            CreateMap<TagEntity, Tag>()
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.AddressSpaceId, opt => opt.MapFrom(src => src.AddressSpaceId))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
                .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.Type))
                .ForMember(dest => dest.CreatedOn, opt => opt.MapFrom(src => src.CreatedOn))
                .ForMember(dest => dest.ModifiedOn, opt => opt.MapFrom(src => src.ModifiedOn))
                .ForMember(dest => dest.KnownValues, opt => opt.MapFrom(src => src.KnownValues))
                .ForMember(dest => dest.Implies, opt => opt.MapFrom(src => src.Implies))
                .ForMember(dest => dest.Attributes, opt => opt.MapFrom(src => src.Attributes));
        }

        private void CreateDtoToEntityMaps()
        {
            // AddressSpace -> AddressSpaceEntity
            CreateMap<AddressSpace, AddressSpaceEntity>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.PartitionKey, opt => opt.MapFrom(src => "AddressSpaces")) // Default partition
                .ForMember(dest => dest.RowKey, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
                .ForMember(dest => dest.CreatedOn, opt => opt.MapFrom(src => src.CreatedOn))
                .ForMember(dest => dest.ModifiedOn, opt => opt.MapFrom(src => src.ModifiedOn))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status))
                .ForMember(dest => dest.Timestamp, opt => opt.Ignore())
                .ForMember(dest => dest.ETag, opt => opt.Ignore());

            // IpAllocation -> IpAllocationEntity
            CreateMap<IpAllocation, IpAllocationEntity>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.PartitionKey, opt => opt.MapFrom(src => src.AddressSpaceId))
                .ForMember(dest => dest.RowKey, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.AddressSpaceId, opt => opt.MapFrom(src => src.AddressSpaceId))
                .ForMember(dest => dest.Prefix, opt => opt.MapFrom(src => src.Prefix))
                .ForMember(dest => dest.ParentId, opt => opt.MapFrom(src => src.ParentId))
                .ForMember(dest => dest.ChildrenIds, opt => opt.MapFrom(src => src.ChildrenIds))
                .ForMember(dest => dest.CreatedOn, opt => opt.MapFrom(src => src.CreatedOn))
                .ForMember(dest => dest.ModifiedOn, opt => opt.MapFrom(src => src.ModifiedOn))
                .ForMember(dest => dest.Tags, opt => opt.MapFrom(src => src.Tags))
                .ForMember(dest => dest.Timestamp, opt => opt.Ignore())
                .ForMember(dest => dest.ETag, opt => opt.Ignore());

            // Tag -> TagEntity
            CreateMap<Tag, TagEntity>()
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.PartitionKey, opt => opt.MapFrom(src => src.AddressSpaceId))
                .ForMember(dest => dest.RowKey, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.AddressSpaceId, opt => opt.MapFrom(src => src.AddressSpaceId))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
                .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.Type))
                .ForMember(dest => dest.CreatedOn, opt => opt.MapFrom(src => src.CreatedOn))
                .ForMember(dest => dest.ModifiedOn, opt => opt.MapFrom(src => src.ModifiedOn))
                .ForMember(dest => dest.KnownValues, opt => opt.MapFrom(src => src.KnownValues))
                .ForMember(dest => dest.Implies, opt => opt.MapFrom(src => src.Implies))
                .ForMember(dest => dest.Attributes, opt => opt.MapFrom(src => src.Attributes))
                .ForMember(dest => dest.Timestamp, opt => opt.Ignore())
                .ForMember(dest => dest.ETag, opt => opt.Ignore());
        }
    }
}