using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PhotoBank.DbContext.Models;
using PhotoBank.Repositories;
using PhotoBank.Services.Models;

namespace PhotoBank.Services.Enrichers
{
    public class ObjectPropertyEnricher : IEnricher
    {
        private readonly IRepository<PropertyName> _propertyNameRepository;

        public ObjectPropertyEnricher(IRepository<PropertyName> propertyNameRepository)
        {
            _propertyNameRepository = propertyNameRepository;
        }

        public EnricherType EnricherType => EnricherType.ObjectProperty;
        public Type[] Dependencies => new[] { typeof(AnalyzeEnricher) };

        public async Task EnrichAsync(Photo photo, SourceDataDto sourceData, CancellationToken cancellationToken = default)
        {
            var incoming = sourceData.ImageAnalysis.Objects
                .Select(o => o.ObjectProperty?.Trim())
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            var query = _propertyNameRepository.GetByCondition(p => incoming.Contains(p.Name));
            List<PropertyName> existing;
            try
            {
                existing = await query.ToListAsync(cancellationToken);
            }
            catch (InvalidOperationException)
            {
                existing = query.ToList();
            }

            var map = existing.ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);

            foreach (var name in incoming)
            {
                if (!map.TryGetValue(name, out var propertyName))
                {
                    propertyName = new PropertyName { Name = name };
                    await _propertyNameRepository.InsertAsync(propertyName);
                    map[name] = propertyName;
                }
            }

            photo.ObjectProperties ??= new List<ObjectProperty>();

            foreach (var detectedObject in sourceData.ImageAnalysis.Objects)
            {
                var propertyName = map[detectedObject.ObjectProperty];
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
