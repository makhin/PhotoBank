using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using Microsoft.EntityFrameworkCore;
using PhotoBank.DbContext.Models;
using PhotoBank.Repositories;
using Category = PhotoBank.DbContext.Models.Category;

namespace PhotoBank.Services
{
    public class CategoryEnricher : IEnricher<ImageAnalysis>
    {
        private readonly IRepository<Category> _categoryRepository;

        public CategoryEnricher(IRepository<Category> categoryRepository)
        {
            _categoryRepository = categoryRepository;
        }
        public void Enrich(Photo photo, ImageAnalysis analysis)
        {
            photo.PhotoCategories = new List<PhotoCategory>();
            foreach (var category in analysis.Categories)
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
