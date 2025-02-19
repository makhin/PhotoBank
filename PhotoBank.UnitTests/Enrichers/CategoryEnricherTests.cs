using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using Moq;
using NUnit.Framework;
using PhotoBank.DbContext.Models;
using PhotoBank.Repositories;
using PhotoBank.Services.Enrichers;
using PhotoBank.Services.Models;
using Category = PhotoBank.DbContext.Models.Category;

namespace PhotoBank.UnitTests.Enrichers
{
    [TestFixture]
    public class CategoryEnricherTests
    {
        private Mock<IRepository<Category>> _mockCategoryRepository;
        private CategoryEnricher _categoryEnricher;

        [SetUp]
        public void Setup()
        {
            _mockCategoryRepository = new Mock<IRepository<Category>>();
            _categoryEnricher = new CategoryEnricher(_mockCategoryRepository.Object);
        }

        [Test]
        public void EnricherType_ShouldReturnCategory()
        {
            // Act
            var result = _categoryEnricher.EnricherType;

            // Assert
            result.Should().Be(EnricherType.Category);
        }

        [Test]
        public void Dependencies_ShouldReturnAnalyzeEnricher()
        {
            // Act
            var result = _categoryEnricher.Dependencies;

            // Assert
            result.Should().ContainSingle()
                .And.Contain(typeof(AnalyzeEnricher));
        }

        [Test]
        public async Task EnrichAsync_ShouldSetPhotoCategories()
        {
            // Arrange
            var photo = new Photo();

            var categories = new List<Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models.Category>()
                    {
                        new() { Name = "Nature", Score = 0.95 },
                        new() { Name = "City", Score = 0.85 }
                    };

            var sourceData = new SourceDataDto
            {
                ImageAnalysis = new ImageAnalysis
                {
                    Categories = categories
                }
            };

            var natureCategory = new Category { Name = "Nature" };
            var cityCategory = new Category { Name = "City" };

            _mockCategoryRepository.Setup(repo => repo.GetAll())
                .Returns((new List<Category> { natureCategory, cityCategory }).AsQueryable);

            // Act
            await _categoryEnricher.EnrichAsync(photo, sourceData);

            // Assert
            photo.PhotoCategories.Should().HaveCount(2);
            photo.PhotoCategories.Should().Contain(pc => pc.Category.Name == "Nature" && pc.Score == 0.95);
            photo.PhotoCategories.Should().Contain(pc => pc.Category.Name == "City" && pc.Score == 0.85);
        }
    }
}
