using System.Collections.Generic;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using PhotoBank.DbContext.Models;

namespace PhotoBank.Services.Enrichers
{
    public class ObjectPropertyEnricher : IEnricher<ImageAnalysis>
    {
        private readonly IGeoWrapper _geoWrapper;

        public ObjectPropertyEnricher(IGeoWrapper geoWrapper)
        {
            _geoWrapper = geoWrapper;
        }

        public void Enrich(Photo photo, ImageAnalysis analysis)
        {
            photo.ObjectProperties = new List<ObjectProperty>();
            foreach (var detectedObject in analysis.Objects)
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
