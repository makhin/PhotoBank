using System.Collections.Generic;
using AutoMapper;
using PhotoBank.DbContext.Models;
using PhotoBank.Dto.View;

namespace PhotoBank.Dto
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<Photo, View.PhotoDto>()
                .ForMember(dest => dest.Captions, opt => opt.MapFrom(src => src.Captions))
                .ForMember(dest => dest.Tags, opt => opt.MapFrom(src => src.PhotoTags))
                .IgnoreAllPropertiesWithAnInaccessibleSetter();

            CreateMap<Caption, string>().ConvertUsing(c => c.Text);
            CreateMap<PhotoTag, string>().ConvertUsing(t => t.Tag.Name);

            CreateMap<Storage, StorageDto>().IgnoreAllPropertiesWithAnInaccessibleSetter();
            CreateMap<Tag, TagDto>().IgnoreAllPropertiesWithAnInaccessibleSetter();

            CreateMap<Photo, View.PhotoItemDto>().IgnoreAllPropertiesWithAnInaccessibleSetter();

            CreateMap<Person, View.PersonDto>()
                .IgnoreAllPropertiesWithAnInaccessibleSetter();

            CreateMap<Face, View.FaceDto>()
                .ForMember(dest => dest.PersonId, opt => opt.MapFrom(src => src.Person == null ? (int?)null : src.Person.Id))
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
