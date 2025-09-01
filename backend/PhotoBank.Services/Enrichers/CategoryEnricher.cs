using System;
using System.Collections.Generic;
using System.Linq;
using PhotoBank.DbContext.Models;
using PhotoBank.Repositories;
using PhotoBank.Services.Models;
using Category = PhotoBank.DbContext.Models.Category;

namespace PhotoBank.Services.Enrichers
{
    public class CategoryEnricher : BaseLookupEnricher<Category, PhotoCategory>
    {
        public CategoryEnricher(IRepository<Category> categoryRepository)
            : base(
                categoryRepository,
                src => src.ImageAnalysis.Categories.Select(c => c.Name),
                name => new Category { Name = name },
                (photo, name, categoryModel, src) =>
                {
                    var cat = src.ImageAnalysis.Categories.First(c => string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase));
                    return new PhotoCategory
                    {
                        Photo = photo,
                        Category = categoryModel,
                        Score = cat.Score
                    };
                })
        {
        }

        public override EnricherType EnricherType => EnricherType.Category;

        protected override ICollection<PhotoCategory> GetCollection(Photo photo) => photo.PhotoCategories ??= new List<PhotoCategory>();
    }
}
