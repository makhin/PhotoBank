using System;
using System.Collections.Generic;
using System.Linq;
using PhotoBank.Services;
using PhotoBank.DbContext.Models;
using PhotoBank.Repositories;
using PhotoBank.Services.Models;

namespace PhotoBank.Services.Enrichers
{
    public class ObjectPropertyEnricher : BaseLookupEnricher<PropertyName, ObjectProperty>
    {
        public ObjectPropertyEnricher(IRepository<PropertyName> propertyNameRepository)
            : base(
                propertyNameRepository,
                src => src.ImageAnalysis.Objects.Select(o => o.ObjectProperty),
                name => new PropertyName { Name = name },
                (photo, name, propertyName, src) =>
                {
                    var detectedObject = src.ImageAnalysis.Objects.First(o => string.Equals(o.ObjectProperty, name, StringComparison.OrdinalIgnoreCase));
                    return new ObjectProperty
                    {
                        PropertyName = propertyName,
                        Rectangle = GeoWrapper.GetRectangle(detectedObject.Rectangle, photo.Scale),
                        Confidence = detectedObject.Confidence
                    };
                })
        {
        }

        public override EnricherType EnricherType => EnricherType.ObjectProperty;

        protected override ICollection<ObjectProperty> GetCollection(Photo photo) => photo.ObjectProperties ??= new List<ObjectProperty>();
    }
}
