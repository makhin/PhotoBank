using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PhotoBank.DbContext.Models;
using PhotoBank.Dto;
using ImageMagick;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;

namespace PhotoBank.Services.Enrichers
{
    public class FaceEnricher : IEnricher
    {
        private readonly IGeoWrapper _geoWrapper;

        public FaceEnricher(IGeoWrapper geoWrapper)
        {
            _geoWrapper = geoWrapper;
        }

        public Type[] Dependencies => Array.Empty<Type>();

        public void Enrich(Photo photo, SourceDataDto sourceData)
        {
            if (!sourceData.ImageAnalysis.Faces.Any())
            {
                return;
            }
            
            MagickImage image = new MagickImage(sourceData.Path);

            photo.Faces = new List<Face>();
            foreach (var faceDescription in sourceData.ImageAnalysis.Faces)
            {
                photo.Faces.Add(new Face()
                {
                    Age = faceDescription.Age,
                    Rectangle = _geoWrapper.GetRectangle(faceDescription.FaceRectangle, sourceData.Scale),
                    Gender = faceDescription.Gender.HasValue ? (int)faceDescription.Gender.Value : (int?)null,
                    Image = ImageHelper.GetFace(sourceData, faceDescription.FaceRectangle)
                });
            }
        }
    }
}
