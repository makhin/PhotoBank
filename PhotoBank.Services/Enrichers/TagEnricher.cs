using System;
using System.Collections.Generic;
using System.Linq;
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

        public void Enrich(Photo photo, SourceDataDto sourceData)

        {
            photo.PhotoTags = new List<PhotoTag>();
            foreach (var tag in sourceData.ImageAnalysis.Tags)
            {
                var tagModel = _tagRepository.GetByCondition(t => t.Name == tag.Name).FirstOrDefault() ?? new Tag
                {
                    Name = tag.Name,
                    Hint = tag.Hint
                };

                var photoTag = new PhotoTag
                {
                    Photo = photo,
                    Tag = tagModel,
                    Confidence = tag.Confidence
                };

                photo.PhotoTags.Add(photoTag);
            }
        }
    }
}
