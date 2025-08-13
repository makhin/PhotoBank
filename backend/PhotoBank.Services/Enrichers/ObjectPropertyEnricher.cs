using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PhotoBank.DbContext.Models;
using PhotoBank.Repositories;
using PhotoBank.Services.Models;

namespace PhotoBank.Services.Enrichers
{
    public class ObjectPropertyEnricher : IEnricher
    {
        private readonly IRepository<PropertyName> _propertyNameRepository;
        private readonly ConcurrentDictionary<string, Lazy<Task<PropertyName>>> _cache =
            new(StringComparer.OrdinalIgnoreCase);

        public ObjectPropertyEnricher(IRepository<PropertyName> propertyNameRepository)
        {
            _propertyNameRepository = propertyNameRepository;
        }

        public EnricherType EnricherType => EnricherType.ObjectProperty;
        public Type[] Dependencies => new Type[1] { typeof(AnalyzeEnricher) };

        public async Task EnrichAsync(Photo photo, SourceDataDto sourceData, CancellationToken cancellationToken = default)
        {
            photo.ObjectProperties = new List<ObjectProperty>();
            foreach (var detectedObject in sourceData.ImageAnalysis.Objects)
            {
                var propertyName = await _cache.GetOrAdd(detectedObject.ObjectProperty, name =>
                    new Lazy<Task<PropertyName>>(async () =>
                    {
                        var existing = _propertyNameRepository.GetByCondition(t => t.Name == name).FirstOrDefault();
                        if (existing == null)
                        {
                            existing = new PropertyName { Name = name };
                            await _propertyNameRepository.InsertAsync(existing);
                        }

                        return existing;
                    })).Value;

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
