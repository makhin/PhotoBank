using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PhotoBank.DbContext.Models;
using PhotoBank.Repositories;
using PhotoBank.Services.Models;
using Category = PhotoBank.DbContext.Models.Category;

namespace PhotoBank.Services.Enrichers
{
    public class CategoryEnricher : IEnricher
    {
        private readonly IRepository<Category> _categoryRepository;
        private readonly ConcurrentDictionary<string, Category> _cache = new(StringComparer.OrdinalIgnoreCase);
        public EnricherType EnricherType => EnricherType.Category;
        public CategoryEnricher(IRepository<Category> categoryRepository)
        {
            _categoryRepository = categoryRepository;
        }
        public Type[] Dependencies => new Type[1] { typeof(AnalyzeEnricher) };

        public Task EnrichAsync(Photo photo, SourceDataDto sourceData, CancellationToken cancellationToken = default)
        {
            photo.PhotoCategories = new List<PhotoCategory>();
            foreach (var category in sourceData.ImageAnalysis.Categories)
            {
                var catModel = _cache.GetOrAdd(category.Name, name =>
                    _categoryRepository.GetByCondition(t => t.Name == name).FirstOrDefault() ?? new Category
                    {
                        Name = name
                    });

                var photoCategory = new PhotoCategory()
                {
                    Photo = photo,
                    Category = catModel,
                    Score = category.Score
                };

                photo.PhotoCategories.Add(photoCategory);
            }
            return Task.CompletedTask;
        }
    }
}
