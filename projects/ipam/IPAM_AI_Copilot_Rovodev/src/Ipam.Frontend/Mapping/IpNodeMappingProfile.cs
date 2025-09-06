using AutoMapper;
using Ipam.DataAccess.Models;
using Ipam.Frontend.Models;

namespace Ipam.Frontend.Mapping
{
    /// <summary>
    /// AutoMapper profile for IP node models
    /// </summary>
    /// <remarks>
    /// Author: IPAM Team
    /// Date: 2024-01-20
    /// </remarks>
    public class IpNodeMappingProfile : Profile
    {
        public IpNodeMappingProfile()
        {
            CreateMap<IpNode, IpNodeResponse>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.RowKey))
                .ForMember(dest => dest.AddressSpaceId, opt => opt.MapFrom(src => src.PartitionKey));

            CreateMap<IpNodeCreateModel, IpNode>()
                .ForMember(dest => dest.PartitionKey, opt => opt.MapFrom(src => src.AddressSpaceId))
                .ForMember(dest => dest.RowKey, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedOn, opt => opt.MapFrom(_ => DateTime.UtcNow))
                .ForMember(dest => dest.ModifiedOn, opt => opt.MapFrom(_ => DateTime.UtcNow));

            CreateMap<IpNodeUpdateModel, IpNode>()
                .ForMember(dest => dest.ModifiedOn, opt => opt.MapFrom(_ => DateTime.UtcNow));
        }
    }
}
