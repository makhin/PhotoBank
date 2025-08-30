using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Rekognition.Model;
using ImageMagick;
using Newtonsoft.Json;
using PhotoBank.DbContext.Models;
using PhotoBank.Services.Models;
using DbFace = PhotoBank.DbContext.Models.Face;

namespace PhotoBank.Services.Enrichers;

public class FaceEnricherAws : IEnricher
{
    private const int MinFaceSize = 36;
    private readonly IFaceServiceAws _faceService;

    public FaceEnricherAws(IFaceServiceAws faceService)
    {
        _faceService = faceService;
    }

    public EnricherType EnricherType => EnricherType.Face;
    public Type[] Dependencies => new[] { typeof(PreviewEnricher), typeof(MetadataEnricher) };

    public async Task EnrichAsync(Photo photo, SourceDataDto sourceData, CancellationToken cancellationToken = default)
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
        photo.Faces = new List<DbFace>();

        foreach (var detectedFace in detectedFaces)
        {
            var previewHeight = sourceData.PreviewImage.Height;
            var previewWidth = sourceData.PreviewImage.Width;

            var faceBytes = await CreateFacePreview(detectedFace.BoundingBox, sourceData.PreviewImage);
            if (!IsAbleToIdentify(previewHeight, previewWidth, detectedFace.BoundingBox))
            {
                if (IsAbleToIdentify(previewHeight, previewWidth, detectedFace.BoundingBox, photo.Scale))
                {
                    faceBytes = await CreateFacePreview(detectedFace.BoundingBox, sourceData.OriginalImage);
                }
                else
                {
                    continue;
                }
            }

            sourceData.FaceImages.Add(faceBytes);

            var face = new DbFace
            {
                PhotoId = photo.Id,
                IdentityStatus = IdentityStatus.NotIdentified,
                Rectangle = GeoWrapper.GetRectangle(previewHeight, previewWidth, detectedFace.BoundingBox, photo.Scale),
                Age = (detectedFace.AgeRange.High + detectedFace.AgeRange.Low) / 2,
                Gender = string.Equals(detectedFace.Gender.Value, "Male", StringComparison.OrdinalIgnoreCase),
                Smile = detectedFace.Smile.Confidence,
                FaceAttributes = JsonConvert.SerializeObject(detectedFace)
            };

            photo.Faces.Add(face);
        }
    }

    private static bool IsAbleToIdentify(uint imageHeight, uint imageWidth, BoundingBox detectedFace, in double scale = 1)
    {
        if (detectedFace.Width.HasValue && detectedFace.Height.HasValue)
            return Math.Round((decimal)(imageHeight * detectedFace.Height / scale)) >= MinFaceSize &&
                   Math.Round((decimal)(imageWidth * detectedFace.Width / scale)) >= MinFaceSize;
        return false;
    }

    private static async Task<byte[]> CreateFacePreview(BoundingBox detectedFace, IMagickImage<byte> image)
    {
        await using var stream = new MemoryStream();
        var faceImage = image.Clone();
        faceImage.Crop(GetMagickGeometry(faceImage.Height, faceImage.Width, detectedFace));
        await faceImage.WriteAsync(stream);
        return stream.ToArray();
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
}
