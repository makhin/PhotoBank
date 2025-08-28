using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Rekognition;
using Amazon.Rekognition.Model;
using ImageMagick;
using Newtonsoft.Json;
using PhotoBank.DbContext.Models;
using PhotoBank.Repositories;
using PhotoBank.Services.Models;
using Minio;
using Minio.DataModel.Args;
using Face = PhotoBank.DbContext.Models.Face;
using Person = PhotoBank.DbContext.Models.Person;

namespace PhotoBank.Services.Enrichers
{
    public class FaceEnricherAws : IEnricher
    {
        private const int MinFaceSize = 36;
        private readonly IFaceServiceAws _faceService;
        private readonly List<Person> _persons;
        private readonly IMinioClient _minio;

        public FaceEnricherAws(IFaceServiceAws faceService, IRepository<Person> personRepository, IMinioClient minio)
        {
            _faceService = faceService;
            _persons = personRepository.GetAll().ToList();
            _minio = minio ?? throw new ArgumentNullException(nameof(minio));
        }
        public EnricherType EnricherType => EnricherType.Face;
        public Type[] Dependencies => new[] { typeof(PreviewEnricher), typeof(MetadataEnricher) };

        public async Task EnrichAsync(Photo photo, SourceDataDto sourceData, CancellationToken cancellationToken = default)
        {
            try
            {
                if (sourceData.PreviewImage is null)
                {
                    photo.FaceIdentifyStatus = FaceIdentifyStatus.NotDetected;
                    return;
                }

                var detectedFaces = await _faceService.DetectFacesAsync(sourceData.PreviewImage.ToByteArray());
                if (detectedFaces.Count == 0)
                {
                    photo.FaceIdentifyStatus = FaceIdentifyStatus.NotDetected;
                    return;
                }

                photo.FaceIdentifyStatus = FaceIdentifyStatus.Detected;
                photo.Faces = new List<Face>();

                foreach (var detectedFace in detectedFaces)
                {
                    var previewImageHeight = sourceData.PreviewImage.Height;
                    var previewImageWidth = sourceData.PreviewImage.Width;

                    var faceBytes = await CreateFacePreview(detectedFace.BoundingBox, sourceData.PreviewImage);
                    if (!IsAbleToIdentify(previewImageHeight, previewImageWidth, detectedFace.BoundingBox))
                    {
                        if (IsAbleToIdentify(previewImageHeight, previewImageWidth, detectedFace.BoundingBox, photo.Scale))
                        {
                            faceBytes = await CreateFacePreview(detectedFace.BoundingBox, sourceData.OriginalImage);
                        }
                        else
                        {
                            continue;
                        }
                    }

                    await using var ms = new MemoryStream(faceBytes);
                    var key = $"faces/{Guid.NewGuid():N}.jpg";
                    var response = await _minio.PutObjectAsync(new PutObjectArgs()
                        .WithBucket("photobank")
                        .WithObject(key)
                        .WithStreamData(ms)
                        .WithObjectSize(ms.Length)
                        .WithContentType("image/jpeg"), cancellationToken);

                    var face = new Face
                    {
                        PhotoId = photo.Id,
                        IdentityStatus = IdentityStatus.NotIdentified,
                        S3Key_Image = key,
                        S3ETag_Image = response?.Etag,
                        Rectangle = GeoWrapper.GetRectangle(previewImageHeight, previewImageWidth, detectedFace.BoundingBox, photo.Scale),
                        Age = (detectedFace.AgeRange.High + detectedFace.AgeRange.Low) / 2,
                        Gender = detectedFace.Gender.Value == GenderType.Male,
                        Smile = detectedFace.Smile.Confidence,
                        FaceAttributes = JsonConvert.SerializeObject(detectedFace),
                    };

                    var matches = await _faceService.SearchUsersByImageAsync(faceBytes);
                    IdentifyFace(face, matches, photo.TakenDate);
                    photo.Faces.Add(face);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private static bool IsAbleToIdentify(uint imageHeight, uint imageWidth, BoundingBox detectedFace, in double scale = 1)
        {
            if (detectedFace.Width.HasValue && detectedFace.Height.HasValue)
                return Math.Round((decimal) (imageHeight * detectedFace.Height / scale)) >= MinFaceSize && Math.Round((decimal)(imageWidth * detectedFace.Width / scale)) >= MinFaceSize;
            return false;
        }

        private static async Task<byte[]> CreateFacePreview(BoundingBox detectedFace, IMagickImage<byte> image)
        {
            await using (var stream = new MemoryStream())
            {
                var faceImage = image.Clone();
                faceImage.Crop(GetMagickGeometry(faceImage.Height, faceImage.Width, detectedFace));
                await faceImage.WriteAsync(stream);
                return stream.ToArray();
            }
        }

        private static MagickGeometry GetMagickGeometry(uint imageHeight, uint imageWidth, BoundingBox detectedFace)
        {
            var height = (uint)(imageHeight * detectedFace.Height);
            var width = (uint)(imageWidth * detectedFace.Width);
            var top = (int)(imageHeight * detectedFace.Top);
            var left = (int)(imageWidth * detectedFace.Left);

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
                if (candidate.Similarity != null) face.IdentifiedWithConfidence = (double) candidate.Similarity;
                face.Person = person;
                return;
            }
        }
    }
}
