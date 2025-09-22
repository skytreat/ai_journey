using AutoMapper;
using Ipam.DataAccess.Entities;
using Ipam.Frontend.Models;
using Ipam.ServiceContract.DTOs;

namespace Ipam.Frontend.Mapping
{
    /// <summary>
    /// AutoMapper configuration profile
    /// </summary>
    /// <remarks>
    /// Author: IPAM Team
    /// Date: 2024-01-20
    /// </remarks>
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            CreateMap<IpAllocationEntity, IpAllocation>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.RowKey))
                .ForMember(dest => dest.AddressSpaceId, opt => opt.MapFrom(src => src.PartitionKey));

            CreateMap<IpNodeCreateModel, IpAllocationEntity>()
                .ForMember(dest => dest.PartitionKey, opt => opt.MapFrom(src => src.AddressSpaceId))
                .ForMember(dest => dest.RowKey, opt => opt.Ignore());
        }
    }
}