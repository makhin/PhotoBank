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
    public sealed class TagEnricher : IEnricher
    {
        private readonly IRepository<Tag> _repo;

        public TagEnricher(IRepository<Tag> repo)
        {
            _repo = repo;
        }

        public EnricherType EnricherType => EnricherType.Tag;

        public Type[] Dependencies => new[] { typeof(AnalyzeEnricher) };

        public async Task EnrichAsync(Photo photo, SourceDataDto src, CancellationToken ct = default)
        {
            var incoming = src.ImageAnalysis.Tags
                .Select(t => t.Name.Trim())
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            var query = _repo.GetByCondition(t => incoming.Contains(t.Name));
            List<Tag> existing;
            try
            {
                existing = await query.AsNoTracking().ToListAsync(ct);
            }
            catch (InvalidOperationException)
            {
                existing = query.AsNoTracking().ToList();
            }

            var map = existing.ToDictionary(t => t.Name, StringComparer.OrdinalIgnoreCase);

            foreach (var name in incoming)
            {
                if (!map.TryGetValue(name, out var tag))
                {
                    tag = new Tag { Name = name };
                    await _repo.InsertAsync(tag);
                    map[name] = tag;
                }
            }

            photo.PhotoTags ??= new List<PhotoTag>();

            foreach (var tag in src.ImageAnalysis.Tags)
            {
                var tagModel = map[tag.Name];
                photo.PhotoTags.Add(new PhotoTag { Photo = photo, Tag = tagModel, Confidence = tag.Confidence });
            }
        }
    }
}
