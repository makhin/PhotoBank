using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PhotoBank.DbContext.Models;
using PhotoBank.Dto;
using PhotoBank.Repositories;

namespace PhotoBank.Services.Enrichers
{
    public class TagEnricher : IEnricher
    {
        private readonly IRepository<Tag> _tagRepository;

        public TagEnricher(IRepository<Tag> tagRepository)
        {
            _tagRepository = tagRepository;
        }

        public Type[] Dependencies => new Type[1] {typeof(AnalyzeEnricher)};

        public async Task Enrich(Photo photo, SourceDataDto sourceData)
        {
            await Task.Run(() =>
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
                    var tagModel = _tagRepository.GetByCondition(t => t.Name == tag.Name).FirstOrDefault() ?? new Tag
                    {
                        Name = tag.Name,
                    };

                    var photoTag = new PhotoTag
                    {
                        Photo = photo,
                        Tag = tagModel,
                        Confidence = tag.Confidence
                    };

                    photo.PhotoTags.Add(photoTag);
                }
            });
        }
    }
}
