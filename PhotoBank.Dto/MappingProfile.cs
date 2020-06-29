using AutoMapper;
using PhotoBank.DbContext.Models;

namespace PhotoBank.Dto
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<Photo, PhotoDto>().IgnoreAllPropertiesWithAnInaccessibleSetter();
            CreateMap<Photo, PhotoItemDto>().IgnoreAllPropertiesWithAnInaccessibleSetter();
        }
    }
}
