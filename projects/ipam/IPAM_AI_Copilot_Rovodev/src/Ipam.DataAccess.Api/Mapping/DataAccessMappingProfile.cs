using AutoMapper;
using Ipam.DataAccess.Models;
using Ipam.DataAccess.Api.Models;

namespace Ipam.DataAccess.Api.Mapping
{
    /// <summary>
    /// AutoMapper profile for Data Access API DTOs
    /// </summary>
    /// <remarks>
    /// Author: IPAM Team
    /// Date: 2024-01-20
    /// </remarks>
    public class DataAccessMappingProfile : Profile
    {
        public DataAccessMappingProfile()
        {
            // Address Space mappings
            CreateMap<AddressSpace, AddressSpaceDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.RowKey));

            CreateMap<CreateAddressSpaceDto, AddressSpace>()
                .ForMember(dest => dest.RowKey, opt => opt.Ignore())
                .ForMember(dest => dest.PartitionKey, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedOn, opt => opt.Ignore())
                .ForMember(dest => dest.ModifiedOn, opt => opt.Ignore())
                .ForMember(dest => dest.Timestamp, opt => opt.Ignore())
                .ForMember(dest => dest.ETag, opt => opt.Ignore());

            CreateMap<UpdateAddressSpaceDto, AddressSpace>()
                .ForMember(dest => dest.RowKey, opt => opt.Ignore())
                .ForMember(dest => dest.PartitionKey, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedOn, opt => opt.Ignore())
                .ForMember(dest => dest.ModifiedOn, opt => opt.Ignore())
                .ForMember(dest => dest.Timestamp, opt => opt.Ignore())
                .ForMember(dest => dest.ETag, opt => opt.Ignore());

            // IP Address mappings
            CreateMap<IPAddress, IPAddressDto>();
            CreateMap<IPAddressTag, IPAddressTagDto>();

            CreateMap<CreateIPAddressDto, IPAddress>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.AddressSpaceId, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedOn, opt => opt.Ignore())
                .ForMember(dest => dest.ModifiedOn, opt => opt.Ignore());

            CreateMap<CreateIPAddressTagDto, IPAddressTag>();

            CreateMap<UpdateIPAddressDto, IPAddress>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.AddressSpaceId, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedOn, opt => opt.Ignore())
                .ForMember(dest => dest.ModifiedOn, opt => opt.Ignore());

            CreateMap<UpdateIPAddressTagDto, IPAddressTag>();

            // Tag mappings
            CreateMap<Tag, TagDto>();

            CreateMap<CreateTagDto, Tag>()
                .ForMember(dest => dest.PartitionKey, opt => opt.Ignore())
                .ForMember(dest => dest.RowKey, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedOn, opt => opt.Ignore())
                .ForMember(dest => dest.ModifiedOn, opt => opt.Ignore())
                .ForMember(dest => dest.Timestamp, opt => opt.Ignore())
                .ForMember(dest => dest.ETag, opt => opt.Ignore());

            CreateMap<UpdateTagDto, Tag>()
                .ForMember(dest => dest.Name, opt => opt.Ignore())
                .ForMember(dest => dest.PartitionKey, opt => opt.Ignore())
                .ForMember(dest => dest.RowKey, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedOn, opt => opt.Ignore())
                .ForMember(dest => dest.ModifiedOn, opt => opt.Ignore())
                .ForMember(dest => dest.Timestamp, opt => opt.Ignore())
                .ForMember(dest => dest.ETag, opt => opt.Ignore());
        }
    }
}