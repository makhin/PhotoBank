using System;
using AutoMapper;
using PhotoBank.AccessControl;
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

            CreateMap<Face, PersonFaceDto>()
                .ForMember(dest => dest.FaceId, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.PersonId, opt => opt.MapFrom(src => src.PersonId!.Value))
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
                .ForMember(dest => dest.PersonId, opt => opt.MapFrom(src => src.PersonId))
                .ForMember(dest => dest.PersonDateOfBirth, opt => opt.MapFrom(src => src.Person.DateOfBirth))
                .ForMember(dest => dest.PhotoTakenDate, opt => opt.MapFrom(src => src.Photo.TakenDate))
                .IgnoreAllPropertiesWithAnInaccessibleSetter();

            CreateMap<AccessProfileStorageAllow, AccessProfileStorageAllowDto>()
                .IgnoreAllPropertiesWithAnInaccessibleSetter();

            CreateMap<AccessProfileStorageAllowDto, AccessProfileStorageAllow>()
                .ForMember(dest => dest.Profile, opt => opt.Ignore())
                .IgnoreAllPropertiesWithAnInaccessibleSetter();

            CreateMap<AccessProfilePersonGroupAllow, AccessProfilePersonGroupAllowDto>()
                .IgnoreAllPropertiesWithAnInaccessibleSetter();

            CreateMap<AccessProfilePersonGroupAllowDto, AccessProfilePersonGroupAllow>()
                .ForMember(dest => dest.Profile, opt => opt.Ignore())
                .IgnoreAllPropertiesWithAnInaccessibleSetter();

            CreateMap<AccessProfileDateRangeAllow, AccessProfileDateRangeAllowDto>()
                .IgnoreAllPropertiesWithAnInaccessibleSetter();

            CreateMap<AccessProfileDateRangeAllowDto, AccessProfileDateRangeAllow>()
                .ForMember(dest => dest.Profile, opt => opt.Ignore())
                .IgnoreAllPropertiesWithAnInaccessibleSetter();

            CreateMap<AccessProfile, AccessProfileDto>()
                .ForMember(dest => dest.Storages, opt => opt.MapFrom(src => src.Storages))
                .ForMember(dest => dest.PersonGroups, opt => opt.MapFrom(src => src.PersonGroups))
                .ForMember(dest => dest.DateRanges, opt => opt.MapFrom(src => src.DateRanges))
                .ForMember(dest => dest.AssignedUsersCount, opt => opt.MapFrom(src => src.UserAssignments.Count))
                .IgnoreAllPropertiesWithAnInaccessibleSetter();

            CreateMap<AccessProfileDto, AccessProfile>()
                .ForMember(dest => dest.Storages,
                    opt => opt.MapFrom(src => src.Storages ?? Array.Empty<AccessProfileStorageAllowDto>()))
                .ForMember(dest => dest.PersonGroups,
                    opt => opt.MapFrom(src => src.PersonGroups ?? Array.Empty<AccessProfilePersonGroupAllowDto>()))
                .ForMember(dest => dest.DateRanges,
                    opt => opt.MapFrom(src => src.DateRanges ?? Array.Empty<AccessProfileDateRangeAllowDto>()))
                .IgnoreAllPropertiesWithAnInaccessibleSetter();
        }
    }
}
