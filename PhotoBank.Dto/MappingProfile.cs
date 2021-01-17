using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using AutoMapper;
using FaceRecognitionDotNet;
using Microsoft.IdentityModel.Tokens;
using PhotoBank.DbContext.Models;

namespace PhotoBank.Dto
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<Photo, PhotoDto>().IgnoreAllPropertiesWithAnInaccessibleSetter();
            CreateMap<Photo, PhotoItemDto>().IgnoreAllPropertiesWithAnInaccessibleSetter();

            CreateMap<Face, FaceDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.PersonId, opt => opt.MapFrom(src => src.PersonGroupFace.PersonId))
                .ForMember(dest => dest.FaceEncoding, opt => opt.MapFrom(src => DeserializeFaceEncoding(src.Encoding)))
                .IgnoreAllPropertiesWithAnInaccessibleSetter();

            CreateMap<Face, PersonFaceDto>()
                .ForMember(dest => dest.FaceId, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.PersonId, opt => opt.MapFrom(src => src.PersonGroupFace.PersonId))
                .ForMember(dest => dest.ExternalGuid, opt => opt.MapFrom(src => src.ExternalGuid))
                .IgnoreAllPropertiesWithAnInaccessibleSetter();

            CreateMap<Person, PersonDto>()
                .ForMember(dest => dest.PersonId, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.ExternalGuid, opt => opt.MapFrom(src => src.ExternalGuid))
                .ForMember(dest => dest.Faces, opt => opt.MapFrom(src => src.PersonGroupFaces))
                .IgnoreAllPropertiesWithAnInaccessibleSetter();
        }

        private static FaceEncoding DeserializeFaceEncoding(byte[] srcEncoding)
        {
            var binaryFormatter = new BinaryFormatter();
            using (var memoryStream = new MemoryStream(srcEncoding))
            {
                return binaryFormatter.Deserialize(memoryStream) as FaceEncoding;
            }
        }
    }
}
