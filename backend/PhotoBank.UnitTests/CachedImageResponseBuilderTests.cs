using System.Net.Mime;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using PhotoBank.Api.Controllers;
using PhotoBank.Services.Api;

namespace PhotoBank.UnitTests;

[TestFixture]
public class CachedImageResponseBuilderTests
{
    private static ControllerBase CreateController(DefaultHttpContext context)
    {
        var controller = new TestController
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = context,
            },
        };

        return controller;
    }

    [Test]
    public void Build_WithMatchingEtagAndCallback_ReturnsNotModifiedAndInvokesCallback()
    {
        // Arrange
        const string etag = "etag-value";
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers.IfNoneMatch = $"\"{etag}\"";
        var controller = CreateController(httpContext);
        var result = new PhotoPreviewResult(etag, null, null);
        var callbackInvoked = false;
        var callbacks = new CachedImageResponseCallbacks(OnNotModified: () => callbackInvoked = true);

        // Act
        var response = CachedImageResponseBuilder.Build(controller, result, callbacks: callbacks);

        // Assert
        response.Should().BeOfType<StatusCodeResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status304NotModified);
        callbackInvoked.Should().BeTrue();
        controller.Response.Headers.ETag.ToString().Should().Be($"\"{etag}\"");
        controller.Response.Headers.CacheControl.ToString().Should().Be("public, max-age=31536000, immutable");
    }

    [Test]
    public void Build_WithMatchingEtagAndNoCallback_LogsInformation()
    {
        // Arrange
        const string etag = "etag-value";
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers.IfNoneMatch = $"\"{etag}\"";
        var controller = CreateController(httpContext);
        var result = new PhotoPreviewResult(etag, null, null);
        var logger = new Mock<ILogger>();

        // Act
        var response = CachedImageResponseBuilder.Build(controller, result, logger.Object);

        // Assert
        response.Should().BeOfType<StatusCodeResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status304NotModified);
    }

    [Test]
    public void Build_WithPreSignedUrlAndCallback_ReturnsMovedPermanentlyAndInvokesCallback()
    {
        // Arrange
        const string url = "https://example.com/photo.jpg";
        var httpContext = new DefaultHttpContext();
        var controller = CreateController(httpContext);
        var result = new PhotoPreviewResult("etag-value", url, null);
        var callbackInvoked = false;
        var callbacks = new CachedImageResponseCallbacks(OnRedirect: () => callbackInvoked = true);

        // Act
        var response = CachedImageResponseBuilder.Build(controller, result, callbacks: callbacks);

        // Assert
        response.Should().BeOfType<StatusCodeResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status301MovedPermanently);
        callbackInvoked.Should().BeTrue();
        controller.Response.Headers.Location.ToString().Should().Be(url);
    }

    [Test]
    public void Build_WithPreSignedUrlAndNoCallback_LogsInformation()
    {
        // Arrange
        const string url = "https://example.com/photo.jpg";
        var httpContext = new DefaultHttpContext();
        var controller = CreateController(httpContext);
        var result = new PhotoPreviewResult("etag-value", url, null);
        var logger = new Mock<ILogger>();

        // Act
        var response = CachedImageResponseBuilder.Build(controller, result, logger.Object);

        // Assert
        response.Should().BeOfType<StatusCodeResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status301MovedPermanently);
    }

    [Test]
    public void Build_WithEmptyIfNoneMatchHeader_StreamsContentAndInvokesCallback()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers.IfNoneMatch = string.Empty;
        var controller = CreateController(httpContext);
        var data = new byte[] { 1, 2, 3 };
        var callbackInvoked = false;
        var callbacks = new CachedImageResponseCallbacks(OnStream: () => callbackInvoked = true);
        var result = new PhotoPreviewResult("etag-value", null, data);

        // Act
        var response = CachedImageResponseBuilder.Build(controller, result, callbacks: callbacks);

        // Assert
        var fileResult = response.Should().BeOfType<FileContentResult>().Subject;
        fileResult.FileContents.Should().Equal(data);
        fileResult.ContentType.Should().Be(MediaTypeNames.Image.Jpeg);
        callbackInvoked.Should().BeTrue();
    }

    [Test]
    public void Build_WithEmptyIfNoneMatchHeaderAndNoCallback_StreamsContentAndLogs()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers.IfNoneMatch = string.Empty;
        var controller = CreateController(httpContext);
        var data = new byte[] { 9, 8, 7 };
        var logger = new Mock<ILogger>();
        var result = new PhotoPreviewResult("etag-value", null, data);

        // Act
        var response = CachedImageResponseBuilder.Build(controller, result, logger.Object);

        // Assert
        response.Should().BeOfType<FileContentResult>()
            .Which.FileContents.Should().Equal(data);
    }

    private sealed class TestController : ControllerBase
    {
    }
}
