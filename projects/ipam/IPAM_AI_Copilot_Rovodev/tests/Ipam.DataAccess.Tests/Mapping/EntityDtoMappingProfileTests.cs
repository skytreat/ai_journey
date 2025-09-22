using Xunit;
using AutoMapper;
using Ipam.DataAccess.Mapping;
using Ipam.DataAccess.Entities;
using Ipam.ServiceContract.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ipam.DataAccess.Tests.Mapping
{
    public class EntityDtoMappingProfileTests
    {
        private readonly IMapper _mapper;

        public EntityDtoMappingProfileTests()
        {
            var configuration = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<EntityDtoMappingProfile>();
            });

            _mapper = configuration.CreateMapper();
        }

        [Fact]
        public void MappingProfile_Configuration_ShouldBeValid()
        {
            _mapper.ConfigurationProvider.AssertConfigurationIsValid();
        }

        [Fact]
        public void Map_TagEntityToDto_ShouldMapAllProperties()
        {
            var entity = new TagEntity
            {
                Name = "Environment",
                AddressSpaceId = "space-1",
                Implies = new Dictionary<string, Dictionary<string, string>>
                {
                    { "Policy", new Dictionary<string, string> { { "Backup", "Required" } } }
                }
            };

            var dto = _mapper.Map<Tag>(entity);

            Assert.NotNull(dto);
            Assert.Equal(entity.Name, dto.Name);
            Assert.Equal(entity.AddressSpaceId, dto.AddressSpaceId);
            Assert.Equal(entity.Implies.Count, dto.Implies.Count);
            Assert.Equal("Required", dto.Implies["Policy"]["Backup"]);
        }

        [Fact]
        public void Map_TagDtoToEntity_ShouldMapAllProperties()
        {
            var dto = new Tag
            {
                Name = "Environment",
                AddressSpaceId = "space-1",
                Implies = new Dictionary<string, Dictionary<string, string>>
                {
                    { "Policy", new Dictionary<string, string> { { "Backup", "Required" } } }
                }
            };

            var entity = _mapper.Map<TagEntity>(dto);

            Assert.NotNull(entity);
            Assert.Equal(dto.Name, entity.Name);
            Assert.Equal(dto.AddressSpaceId, entity.AddressSpaceId);
            Assert.Equal(dto.Implies.Count, entity.Implies.Count);
            Assert.Equal("Required", entity.Implies["Policy"]["Backup"]);
        }

        [Fact]
        public void Map_TagDtoToEntityAndBack_ShouldPreserveData()
        {
            var originalDto = new Tag
            {
                Name = "Environment",
                AddressSpaceId = "space-1",
                Implies = new Dictionary<string, Dictionary<string, string>>
                {
                    { "Policy", new Dictionary<string, string> { { "Backup", "Required" } } }
                }
            };

            var entity = _mapper.Map<TagEntity>(originalDto);
            var resultDto = _mapper.Map<Tag>(entity);

            Assert.Equal(originalDto.Name, resultDto.Name);
            Assert.Equal(originalDto.Implies.Count, resultDto.Implies.Count);
            Assert.Equal(originalDto.Implies["Policy"]["Backup"], resultDto.Implies["Policy"]["Backup"]);
        }
    }
}
