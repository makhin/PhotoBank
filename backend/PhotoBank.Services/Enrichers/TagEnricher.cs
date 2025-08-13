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
    public class TagEnricher : IEnricher
    {
        private readonly IRepository<Tag> _tagRepository;
        private readonly ConcurrentDictionary<string, Tag> _cache = new(StringComparer.OrdinalIgnoreCase);

        public TagEnricher(IRepository<Tag> tagRepository)
        {
            _tagRepository = tagRepository;
        }
        public EnricherType EnricherType => EnricherType.Tag;
        public Type[] Dependencies => new Type[1] { typeof(AnalyzeEnricher) };

        public Task EnrichAsync(Photo photo, SourceDataDto sourceData, CancellationToken cancellationToken = default)
        {
            photo.PhotoTags = new List<PhotoTag>();

            // Workaround for bug, when service returns the same tags
            var tags = sourceData.ImageAnalysis.Tags.GroupBy(t => t.Name.ToLower()).Select(t =>
                new
                {
                    Name = t.Key,
                    Confidence = t.Max(v => v.Confidence)
                });

            foreach (var tag in tags)
            {
                var tagModel = _cache.GetOrAdd(tag.Name, name =>
                    _tagRepository.GetByCondition(t => t.Name == name).FirstOrDefault() ?? new Tag
                    {
                        Name = name,
                    });

                var photoTag = new PhotoTag
                {
                    Photo = photo,
                    Tag = tagModel,
                    Confidence = tag.Confidence
                };

                photo.PhotoTags.Add(photoTag);
            }

            return Task.CompletedTask;
        }
    }
}
