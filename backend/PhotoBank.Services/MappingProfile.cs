using System;
using AutoMapper;
using PhotoBank.DbContext.Models;
using PhotoBank.Services.Models;
using PhotoBank.ViewModel.Dto;

namespace PhotoBank.Services
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<Photo, PhotoDto>()
                .ForMember(dest => dest.Captions, opt => opt.MapFrom(src => src.Captions))
                .ForMember(dest => dest.Tags, opt => opt.MapFrom(src => src.PhotoTags))
                .ForMember(dest => dest.Location,
                    opt => opt.MapFrom(src => src.Location == null
                        ? null
                        : new GeoPointDto
                        {
                            Latitude = src.Location.Coordinate.X,
                            Longitude = src.Location.Coordinate.Y
                        }))
                .ForMember(dest => dest.PreviewUrl, opt => opt.Ignore())
                .ForMember(dest => dest.S3Key_Preview, opt => opt.MapFrom(s => s.S3Key_Preview))
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
                .ForMember(dest => dest.ThumbnailUrl, opt => opt.Ignore())
                .ForMember(dest => dest.S3Key_Thumbnail, opt => opt.MapFrom(s => s.S3Key_Thumbnail))
                .ForMember(dest => dest.Tags, opt => opt.MapFrom(src => src.PhotoTags))
                .ForMember(dest => dest.Persons, opt => opt.MapFrom(src => src.Faces))
                .ForMember(dest => dest.Captions, opt => opt.MapFrom(src => src.Captions))
                .ForMember(dest => dest.StorageName, opt => opt.MapFrom(src => src.Storage.Name))
                .IgnoreAllPropertiesWithAnInaccessibleSetter();

            CreateMap<Photo, PathDto>()
                .ForMember(dest => dest.StorageId, opt => opt.MapFrom(src => src.StorageId))
                .ForMember(dest => dest.Path, opt => opt.MapFrom(src => src.RelativePath))
                .IgnoreAllPropertiesWithAnInaccessibleSetter();

            CreateMap<Person, PersonDto>()
                .IgnoreAllPropertiesWithAnInaccessibleSetter();

            CreateMap<PersonGroup, PersonGroupDto>()
                .ForMember(dest => dest.Persons,
                    opt => opt.MapFrom(src => src.Persons!))
                .IgnoreAllPropertiesWithAnInaccessibleSetter();

            CreateMap<PersonFace, PersonFaceDto>()
                .IgnoreAllPropertiesWithAnInaccessibleSetter();

            CreateMap<PersonFaceDto, PersonFace>()
                .ForMember(dest => dest.Person, opt => opt.Ignore())
                .ForMember(dest => dest.Face, opt => opt.Ignore())
                .IgnoreAllPropertiesWithAnInaccessibleSetter();

            CreateMap<Face, FaceIdentityDto>()
                .ForMember(dest => dest.Person, opt => opt.MapFrom(src => src.Person))
                .IgnoreAllPropertiesWithAnInaccessibleSetter();

            CreateMap<Face, ViewModel.Dto.FaceDto>()
                .ForMember(dest => dest.PersonId, opt => opt.MapFrom(src => src.Person == null ? (int?)null : src.Person.Id))
                .ForMember(dest => dest.FaceBox, opt => opt.MapFrom(src => FaceHelper.GetFaceBox(src.Rectangle, src.Photo)))
                .ForMember(dest => dest.FriendlyFaceAttributes, opt => opt.MapFrom(src => FaceHelper.GetFriendlyFaceAttributes(src.FaceAttributes)))
                .IgnoreAllPropertiesWithAnInaccessibleSetter();

            CreateMap<Face, Models.FaceDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.PersonId, opt => opt.MapFrom(src => src.PersonFace.PersonId))
                .ForMember(dest => dest.PersonDateOfBirth, opt => opt.MapFrom(src => src.Person.DateOfBirth))
                .ForMember(dest => dest.PhotoTakenDate, opt => opt.MapFrom(src => src.Photo.TakenDate))
                .IgnoreAllPropertiesWithAnInaccessibleSetter();
        }
    }
}
