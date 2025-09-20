using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using PhotoBank.Api.Controllers;
using PhotoBank.Services.Api;
using PhotoBank.ViewModel.Dto;

namespace PhotoBank.UnitTests;

[TestFixture]
public class PhotosControllerTests
{
    [Test]
    public async Task SearchPhotos_CallsServiceWithFilter()
    {
        // Arrange
        var logger = Mock.Of<ILogger<PhotosController>>();
        var filter = new FilterDto { PersonNames = ["John"], TagNames = ["car"] };

        var page = new PageResponse<PhotoItemDto>();
        var photoService = new Mock<IPhotoService>();
        photoService
            .Setup(s => s.GetAllPhotosAsync(It.IsAny<FilterDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(page);

        var controller = new PhotosController(logger, photoService.Object);

        // Act
        var result = await controller.SearchPhotos(filter);

        // Assert
        photoService.Verify(s => s.GetAllPhotosAsync(filter, It.IsAny<CancellationToken>()), Times.Once);
        result.Result.Should().BeOfType<OkObjectResult>();
    }
}

