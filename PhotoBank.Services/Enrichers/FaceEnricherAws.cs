using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Amazon.Rekognition;
using Amazon.Rekognition.Model;
using ImageMagick;
using Newtonsoft.Json;
using PhotoBank.DbContext.Models;
using PhotoBank.Dto.Load;
using PhotoBank.Repositories;
using Face = PhotoBank.DbContext.Models.Face;
using Person = PhotoBank.DbContext.Models.Person;

namespace PhotoBank.Services.Enrichers
{
    public class FaceEnricherAws : IEnricher
    {
        private const int MinFaceSize = 36;
        private readonly IFaceServiceAws _faceService;
        private readonly List<Person> _persons;

        public FaceEnricherAws(IFaceServiceAws faceService, IRepository<Person> personRepository)
        {
            _faceService = faceService;
            _persons = personRepository.GetAll().ToList();
        }

        public Type[] Dependencies => new[] { typeof(PreviewEnricher), typeof(MetadataEnricher) };

        public async Task Enrich(Photo photo, SourceDataDto sourceData)
        {
            try
            {
                var detectedFaces = await _faceService.DetectFacesAsync(photo.PreviewImage);
                if (detectedFaces.Count == 0)
                {
                    photo.FaceIdentifyStatus = FaceIdentifyStatus.NotDetected;
                    return;
                }

                photo.FaceIdentifyStatus = FaceIdentifyStatus.Detected;
                photo.Faces = new List<Face>();

                foreach (var detectedFace in detectedFaces)
                {
                    var face = new Face
                    {
                        PhotoId = photo.Id,
                        IdentityStatus = IdentityStatus.NotIdentified,
                        Image = await CreateFacePreview(detectedFace.BoundingBox, sourceData.PreviewImage, 1),
                        Rectangle = GeoWrapper.GetRectangle(photo.Height.Value, photo.Width.Value, detectedFace.BoundingBox, photo.Scale),
                        Age = (detectedFace.AgeRange.High + detectedFace.AgeRange.Low) / 2,
                        Gender = detectedFace.Gender.Value == GenderType.Male,
                        Smile = detectedFace.Smile.Confidence,
                        FaceAttributes = JsonConvert.SerializeObject(new
                        {
                            detectedFace.AgeRange,
                            detectedFace.Beard,
                            detectedFace.Confidence,
                            detectedFace.Emotions,
                            detectedFace.EyeDirection,
                            detectedFace.Eyeglasses,
                            detectedFace.EyesOpen,
                            detectedFace.FaceOccluded,
                            detectedFace.Gender,
                            detectedFace.MouthOpen,
                            detectedFace.Mustache,
                            detectedFace.Pose,
                            detectedFace.Smile,
                            detectedFace.Sunglasses,
                        })
                    };

                    byte[] faceImage;
                    if (IsAbleToIdentify(sourceData.PreviewImage.Height, sourceData.PreviewImage.Width, detectedFace.BoundingBox))
                    {
                        faceImage = face.Image;
                    }
                    else if (IsAbleToIdentify(photo.Height.Value, photo.Width.Value, detectedFace.BoundingBox, photo.Scale))
                    {
                        faceImage = await CreateFacePreview(detectedFace.BoundingBox, sourceData.OriginalImage, photo.Scale);
                    }
                    else
                    {
                        continue;
                    }

                    var matches = await _faceService.SearchUsersByImageAsync(faceImage);
                    IdentifyFace(face, matches, photo.TakenDate);
                    photo.Faces.Add(face);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private static bool IsAbleToIdentify(int imageHeight, int imageWidth, BoundingBox detectedFace)
        {
            return IsAbleToIdentify(imageHeight, imageWidth, detectedFace, 1);
        }

        private static bool IsAbleToIdentify(int imageHeight, int imageWidth, BoundingBox detectedFace, in double scale)
        {
            return Math.Round(imageHeight * detectedFace.Height / scale) >= MinFaceSize && Math.Round(imageWidth * detectedFace.Width / scale) >= MinFaceSize;
        }

        private static async Task<byte[]> CreateFacePreview(BoundingBox detectedFace, IMagickImage<byte> image, double photoScale)
        {
            await using (var stream = new MemoryStream())
            {
                var faceImage = image.Clone();
                faceImage.Crop(GetMagickGeometry(faceImage.Height, faceImage.Width, detectedFace, photoScale));
                await faceImage.WriteAsync(stream);
                return stream.ToArray();
            }
        }

        private static MagickGeometry GetMagickGeometry(int imageHeight, int imageWidth, BoundingBox detectedFace, double photoScale)
        {
            var height = (int)(imageHeight * detectedFace.Height / photoScale);
            var width = (int)(imageWidth * detectedFace.Width / photoScale);
            var top = (int)(imageHeight * detectedFace.Top / photoScale);
            var left = (int)(imageWidth * detectedFace.Left / photoScale);

            var geometry = new MagickGeometry(width, height)
            {
                IgnoreAspectRatio = true,
                Y = top,
                X = left
            };
            return geometry;
        }

        private void IdentifyFace(Face face, IEnumerable<UserMatch> userMatches, DateTime? photoTakenDate)
        {
            foreach (var candidate in userMatches.OrderByDescending(x => x.Similarity))
            {
                var person = _persons.Single(p => p.Id.ToString() == candidate.User.UserId);

                if (person.DateOfBirth != null && photoTakenDate > new DateTime(1990, 1, 1) && photoTakenDate < person.DateOfBirth)
                {
                    continue;
                }

                face.IdentityStatus = IdentityStatus.Identified;
                face.IdentifiedWithConfidence = candidate.Similarity;
                face.Person = person;
            }
        }
    }
}
