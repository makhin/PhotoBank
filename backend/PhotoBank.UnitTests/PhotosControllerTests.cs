using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using PhotoBank.Api.Controllers;
using PhotoBank.Services.Api;
using PhotoBank.Services.Search;
using PhotoBank.ViewModel.Dto;

namespace PhotoBank.UnitTests;

[TestFixture]
public class PhotosControllerTests
{
    [Test]
    public async Task SearchPhotos_NormalizesFilter()
    {
        // Arrange
        var logger = Mock.Of<ILogger<PhotosController>>();
        var filter = new FilterDto { PersonNames = ["John"], TagNames = ["car"] };

        var normalizer = new Mock<ISearchFilterNormalizer>();
        normalizer
            .Setup(n => n.NormalizeAsync(It.IsAny<FilterDto>(), It.IsAny<CancellationToken>()))
            .Callback<FilterDto, CancellationToken>((f, _) =>
            {
                f.Persons = new[] { 1 };
                f.Tags = new[] { 2 };
            })
            .ReturnsAsync((FilterDto f, CancellationToken _) => f);

        var page = new PageResponse<PhotoItemDto>();
        var photoService = new Mock<IPhotoService>();
        photoService
            .Setup(s => s.GetAllPhotosAsync(It.IsAny<FilterDto>()))
            .ReturnsAsync(page);

        var controller = new PhotosController(logger, photoService.Object, normalizer.Object);

        // Act
        var result = await controller.SearchPhotos(filter);

        // Assert
        normalizer.Verify(n => n.NormalizeAsync(filter, It.IsAny<CancellationToken>()), Times.Once);
        photoService.Verify(s => s.GetAllPhotosAsync(It.Is<FilterDto>(f =>
            f.Persons!.SequenceEqual(new[] { 1 }) && f.Tags!.SequenceEqual(new[] { 2 })
        )), Times.Once);
        result.Result.Should().BeOfType<OkObjectResult>();
    }
}

