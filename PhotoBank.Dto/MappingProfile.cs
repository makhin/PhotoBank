using AutoMapper;
using PhotoBank.DbContext.Models;

namespace PhotoBank.Dto
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<Photo, View.PhotoDto>()
                .IgnoreAllPropertiesWithAnInaccessibleSetter();
            CreateMap<Photo, View.PhotoItemDto>().IgnoreAllPropertiesWithAnInaccessibleSetter();

            CreateMap<Face, View.FaceDto>()
                .ForMember(dest => dest.PersonId, opt => opt.MapFrom(src => src.Person == null ? (int?)null : src.Person.Id))
                .ForMember(dest => dest.PersonName, opt => opt.MapFrom(src => src.Person == null ? string.Empty : src.Person.Name))
                .ForMember(dest => dest.FaceBox, opt => opt.MapFrom(src => GeoWrapper.GetFaceBox(src.Rectangle, src.Photo.Scale)))
                .IgnoreAllPropertiesWithAnInaccessibleSetter();

            CreateMap<Face, Load.FaceDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.PersonId, opt => opt.MapFrom(src => src.PersonGroupFace.PersonId))
                .ForMember(dest => dest.PersonDateOfBirth, opt => opt.MapFrom(src => src.Person.DateOfBirth))
                .ForMember(dest => dest.PhotoTakenDate, opt => opt.MapFrom(src => src.Photo.TakenDate))
                .IgnoreAllPropertiesWithAnInaccessibleSetter();
        }
    }
}
