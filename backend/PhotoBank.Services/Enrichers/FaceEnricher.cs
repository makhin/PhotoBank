using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ImageMagick;
using Microsoft.Azure.CognitiveServices.Vision.Face.Models;
using Newtonsoft.Json;
using PhotoBank.DbContext.Models;
using PhotoBank.Services.Enrichers.Services;
using PhotoBank.Services.Models;
using Gender = Microsoft.Azure.CognitiveServices.Vision.Face.Models.Gender;

namespace PhotoBank.Services.Enrichers;

public class FaceEnricher : IEnricher
{
    private readonly IFaceService _faceService;
    private readonly IFacePreviewService _facePreviewService;

    public FaceEnricher(IFaceService faceService, IFacePreviewService facePreviewService)
    {
        _faceService = faceService;
        _facePreviewService = facePreviewService;
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
        if (!detectedFaces.Any())
        {
            photo.FaceIdentifyStatus = FaceIdentifyStatus.NotDetected;
            return;
        }

        photo.FaceIdentifyStatus = FaceIdentifyStatus.Detected;
        photo.Faces = new List<Face>();

        foreach (var detectedFace in detectedFaces)
        {
            var bytes = await _facePreviewService.CreateFacePreview(detectedFace, sourceData.PreviewImage, 1);
            sourceData.FaceImages.Add(bytes);

            var face = new Face
            {
                PhotoId = photo.Id,
                IdentityStatus = IdentityStatus.NotIdentified,
                Rectangle = GeoWrapper.GetRectangle(detectedFace.FaceRectangle, photo.Scale)
            };

            var attributes = detectedFace.FaceAttributes;
            if (attributes != null)
            {
                face.Age = attributes.Age;
                face.Gender = attributes.Gender == Gender.Male;
                face.Smile = attributes.Smile;
                face.FaceAttributes = JsonConvert.SerializeObject(attributes);
            }

            photo.Faces.Add(face);
        }
    }

}
