using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PhotoBank.DbContext.Models;
using PhotoBank.Repositories;
using PhotoBank.Services.Models;
using Category = PhotoBank.DbContext.Models.Category;

namespace PhotoBank.Services.Enrichers
{
    public class CategoryEnricher : IEnricher
    {
        private readonly IRepository<Category> _categoryRepository;
        public EnricherType EnricherType => EnricherType.Category;

        public CategoryEnricher(IRepository<Category> categoryRepository)
        {
            _categoryRepository = categoryRepository;
        }

        public Type[] Dependencies => new[] { typeof(AnalyzeEnricher) };

        public async Task EnrichAsync(Photo photo, SourceDataDto sourceData, CancellationToken cancellationToken = default)
        {
            var incoming = sourceData.ImageAnalysis.Categories
                .Select(c => c.Name.Trim())
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            var query = _categoryRepository.GetByCondition(c => incoming.Contains(c.Name));
            List<Category> existing;
            try
            {
                existing = await query.AsNoTracking().ToListAsync(cancellationToken);
            }
            catch (InvalidOperationException)
            {
                existing = query.AsNoTracking().ToList();
            }

            var map = existing.ToDictionary(c => c.Name, StringComparer.OrdinalIgnoreCase);

            foreach (var name in incoming)
            {
                if (!map.TryGetValue(name, out var category))
                {
                    category = new Category { Name = name };
                    await _categoryRepository.InsertAsync(category);
                    map[name] = category;
                }
            }

            photo.PhotoCategories ??= new List<PhotoCategory>();

            foreach (var category in sourceData.ImageAnalysis.Categories)
            {
                var catModel = map[category.Name];
                photo.PhotoCategories.Add(new PhotoCategory
                {
                    Photo = photo,
                    Category = catModel,
                    Score = category.Score
                });
            }
        }
    }
}
