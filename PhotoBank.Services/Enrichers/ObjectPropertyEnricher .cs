using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PhotoBank.DbContext.Models;
using PhotoBank.Dto.Load;
using PhotoBank.Repositories;

namespace PhotoBank.Services.Enrichers
{
    public class ObjectPropertyEnricher : IEnricher
    {
        private readonly IRepository<PropertyName> _propertyNameRepository;

        public ObjectPropertyEnricher(IRepository<PropertyName> propertyNameRepository)
        {
            _propertyNameRepository = propertyNameRepository;
        }

        public Type[] Dependencies => new Type[1] { typeof(AnalyzeEnricher) };

        public async Task Enrich(Photo photo, SourceDataDto sourceData)
        {
            photo.ObjectProperties = new List<ObjectProperty>();
            foreach (var detectedObject in sourceData.ImageAnalysis.Objects)
            {
                var propertyName = _propertyNameRepository.GetByCondition(t => t.Name == detectedObject.ObjectProperty).FirstOrDefault();

                if (propertyName == null)
                {
                    propertyName = new PropertyName
                    {
                        Name = detectedObject.ObjectProperty,
                    };

                    await _propertyNameRepository.InsertAsync(propertyName);
                }

                photo.ObjectProperties.Add(new ObjectProperty
                {
                    PropertyName = propertyName,
                    Rectangle = GeoWrapper.GetRectangle(detectedObject.Rectangle, photo.Scale),
                    Confidence = detectedObject.Confidence
                });
            }
        }
    }
}
