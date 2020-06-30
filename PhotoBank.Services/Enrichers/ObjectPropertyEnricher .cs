using System;
using System.Collections.Generic;
using PhotoBank.DbContext.Models;
using PhotoBank.Dto;

namespace PhotoBank.Services.Enrichers
{
    public class ObjectPropertyEnricher : IEnricher
    {
        private readonly IGeoWrapper _geoWrapper;

        public ObjectPropertyEnricher(IGeoWrapper geoWrapper)
        {
            _geoWrapper = geoWrapper;
        }

        public Type[] Dependencies => new Type[0];

        public void Enrich(Photo photo, SourceDataDto sourceData)

        {
            photo.ObjectProperties = new List<ObjectProperty>();
            foreach (var detectedObject in sourceData.ImageAnalysis.Objects)
            {
                photo.ObjectProperties.Add(new ObjectProperty()
                {
                    Name = detectedObject.ObjectProperty,
                    Rectangle = _geoWrapper.GetRectangle(detectedObject.Rectangle),
                    Confidence = detectedObject.Confidence
                });
            }
        }
    }
}
