using System;
using System.Collections.Generic;
using System.Linq;
using PhotoBank.DbContext.Models;
using PhotoBank.Dto;
using PhotoBank.Repositories;
using Category = PhotoBank.DbContext.Models.Category;

namespace PhotoBank.Services.Enrichers
{
    public class CategoryEnricher : IEnricher
    {
        private readonly IRepository<Category> _categoryRepository;

        public CategoryEnricher(IRepository<Category> categoryRepository)
        {
            _categoryRepository = categoryRepository;
        }
        public Type[] Dependencies => new Type[1] { typeof(AnalyzeEnricher) };

        public void Enrich(Photo photo, SourceDataDto sourceData)

        {
            photo.PhotoCategories = new List<PhotoCategory>();
            foreach (var category in sourceData.ImageAnalysis.Categories)
            {
                var catModel = _categoryRepository.GetByCondition(t => t.Name == category.Name).FirstOrDefault();

                if (catModel == null)
                {
                    catModel = new Category
                    {
                        Name = category.Name
                    };
                }

                var photoCategory = new PhotoCategory()
                {
                    Photo = photo,
                    Category = catModel,
                    Score = category.Score
                };
                
                photo.PhotoCategories.Add(photoCategory);
            }
        }
    }
}
