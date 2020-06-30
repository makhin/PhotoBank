using System;
using System.Collections.Generic;
using PhotoBank.DbContext.Models;
using PhotoBank.Dto;

namespace PhotoBank.Services.Enrichers
{
    public class FaceEnricher : IEnricher
    {
        private readonly IGeoWrapper _geoWrapper;

        public FaceEnricher(IGeoWrapper geoWrapper)
        {
            _geoWrapper = geoWrapper;
        }

        public Type[] Dependencies => new Type[0];

        public void Enrich(Photo photo, SourceDataDto sourceData)

        {
            photo.Faces = new List<Face>();
            foreach (var faceDescription in sourceData.ImageAnalysis.Faces)
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
