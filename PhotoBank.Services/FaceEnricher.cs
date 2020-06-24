using System.Collections.Generic;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using PhotoBank.DbContext.Models;

namespace PhotoBank.Services
{
    public class FaceEnricher : IEnricher<ImageAnalysis>
    {
        private readonly IGeoWrapper _geoWrapper;

        public FaceEnricher(IGeoWrapper geoWrapper)
        {
            _geoWrapper = geoWrapper;
        }

        public void Enrich(Photo photo, ImageAnalysis analysis)
        {
            photo.Faces = new List<Face>();
            foreach (var faceDescription in analysis.Faces)
            {
                photo.Faces.Add(new Face()
                {
                    Age = faceDescription.Age,
                    Rectangle = _geoWrapper.GetRectangle(faceDescription.FaceRectangle),
                    Gender = faceDescription.Gender.HasValue ? (int)faceDescription.Gender.Value : (int?)null
                });
            }
        }
    }
}
