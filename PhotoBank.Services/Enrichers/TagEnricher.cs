using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using PhotoBank.DbContext.Models;
using PhotoBank.Repositories;

namespace PhotoBank.Services.Enrichers
{
    public class TagEnricher : IEnricher<ImageAnalysis>
    {
        private readonly IRepository<Tag> _tagRepository;

        public TagEnricher(IRepository<Tag> tagRepository)
        {
            _tagRepository = tagRepository;
        }
        public void Enrich(Photo photo, ImageAnalysis analysis)
        {
            photo.PhotoTags = new List<PhotoTag>();
            foreach (var tag in analysis.Tags)
            {
                var tagModel = _tagRepository.GetByCondition(t => t.Name == tag.Name).FirstOrDefault();
                if (tagModel == null)
                {
                    tagModel = new Tag
                    {
                        Name = tag.Name,
                        Hint = tag.Hint
                    };
                }

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
