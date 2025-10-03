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

            CreateMap<PhotoTag, int>().ConvertUsing(t => t.TagId);
            CreateMap<Face, int>().ConvertUsing(t => t.PersonId ?? 0);

            CreateMap<Face, FaceDto>()
                .ForMember(dest => dest.PersonId, opt => opt.MapFrom(src => src.PersonId))
                .ForMember(dest => dest.ImageUrl, opt => opt.Ignore())
                .ForMember(dest => dest.FaceBox, opt => opt.MapFrom(src => FaceHelper.GetFaceBox(src.Rectangle, src.Photo)))
                .ForMember(dest => dest.FriendlyFaceAttributes, opt => opt.MapFrom(src => FaceHelper.GetFriendlyFaceAttributes(src.FaceAttributes)))
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
                    opt => opt.MapFrom(src => src.Storages))
                .ForMember(dest => dest.PersonGroups,
                    opt => opt.MapFrom(src => src.PersonGroups))
                .ForMember(dest => dest.DateRanges,
                    opt => opt.MapFrom(src => src.DateRanges))
                .ForMember(dest => dest.UserAssignments, opt => opt.Ignore())
                .IgnoreAllPropertiesWithAnInaccessibleSetter();
        }
    }
}
