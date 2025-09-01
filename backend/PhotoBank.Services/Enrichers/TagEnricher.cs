using System;
using System.Collections.Generic;
using System.Linq;
using PhotoBank.DbContext.Models;
using PhotoBank.Repositories;
using PhotoBank.Services.Models;

namespace PhotoBank.Services.Enrichers
{
    public sealed class TagEnricher : BaseLookupEnricher<Tag, PhotoTag>
    {
        public TagEnricher(IRepository<Tag> repo)
            : base(
                repo,
                src => src.ImageAnalysis.Tags.Select(t => t.Name),
                name => new Tag { Name = name },
                (photo, name, tagModel, src) =>
                {
                    var tag = src.ImageAnalysis.Tags.First(t => string.Equals(t.Name, name, StringComparison.OrdinalIgnoreCase));
                    return new PhotoTag { Photo = photo, Tag = tagModel, Confidence = tag.Confidence };
                })
        {
        }

        public override EnricherType EnricherType => EnricherType.Tag;

        protected override ICollection<PhotoTag> GetCollection(Photo photo) => photo.PhotoTags ??= new List<PhotoTag>();
    }
}
