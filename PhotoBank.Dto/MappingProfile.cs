using System.Collections.Generic;
using AutoMapper;
using NetTopologySuite.Geometries;
using PhotoBank.DbContext.Models;
using PhotoBank.Dto.View;

namespace PhotoBank.Dto
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<Photo, PhotoDto>()
                .ForMember(dest => dest.Captions, opt => opt.MapFrom(src => src.Captions))
                .ForMember(dest => dest.Tags, opt => opt.MapFrom(src => src.PhotoTags))
                .IgnoreAllPropertiesWithAnInaccessibleSetter();

            CreateMap<Caption, string>().ConvertUsing(c => c.Text);
            CreateMap<PhotoTag, string>().ConvertUsing(t => t.Tag.Name);

            CreateMap<Storage, StorageDto>().IgnoreAllPropertiesWithAnInaccessibleSetter();
            CreateMap<Tag, TagDto>().IgnoreAllPropertiesWithAnInaccessibleSetter();

            CreateMap<PhotoTag, TagItemDto>()
                .ForMember(dest => dest.TagId, opt => opt.MapFrom(src => src.TagId))
                .IgnoreAllPropertiesWithAnInaccessibleSetter();

            CreateMap<Face, PersonItemDto>()
                .ForMember(dest => dest.PersonId, opt => opt.MapFrom(src => src.PersonId))
                .IgnoreAllPropertiesWithAnInaccessibleSetter();

            CreateMap<Photo, PhotoItemDto>()
                .ForMember(dest => dest.Tags, opt => opt.MapFrom(src => src.PhotoTags))
                .ForMember(dest => dest.Persons, opt => opt.MapFrom(src => src.Faces))
                .IgnoreAllPropertiesWithAnInaccessibleSetter();

            CreateMap<Photo, PathDto>()
                .ForMember(dest => dest.StorageId, opt => opt.MapFrom(src => src.StorageId))
                .ForMember(dest => dest.Path, opt => opt.MapFrom(src => src.RelativePath))
                .IgnoreAllPropertiesWithAnInaccessibleSetter();

            CreateMap<Person, PersonDto>()
                .IgnoreAllPropertiesWithAnInaccessibleSetter();

            CreateMap<Face, FaceDto>()
                .ForMember(dest => dest.PersonId, opt => opt.MapFrom(src => src.Person == null ? (int?)null : src.Person.Id))
                .ForMember(dest => dest.FaceBox, opt => opt.MapFrom(src => GetFaceBox(src.Rectangle, src.Photo.Scale)))
                .IgnoreAllPropertiesWithAnInaccessibleSetter();

            CreateMap<Face, Load.FaceDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.PersonId, opt => opt.MapFrom(src => src.PersonGroupFace.PersonId))
                .ForMember(dest => dest.PersonDateOfBirth, opt => opt.MapFrom(src => src.Person.DateOfBirth))
                .ForMember(dest => dest.PhotoTakenDate, opt => opt.MapFrom(src => src.Photo.TakenDate))
                .IgnoreAllPropertiesWithAnInaccessibleSetter();
        }

        private static FaceBoxDto GetFaceBox(Geometry geometry, double scale = 1)
        {
            const string suffix = "px";
            return new FaceBoxDto
            {
                Left = (int)(geometry.Coordinates[0].X * scale) + suffix,
                Top = (int)(geometry.Coordinates[0].Y * scale) + suffix,
                Width = (int)((geometry.Coordinates[1].X - geometry.Coordinates[0].X) * scale) + suffix,
                Height = (int)((geometry.Coordinates[3].Y - geometry.Coordinates[0].Y) * scale) + suffix
            };
        }
    }
}
