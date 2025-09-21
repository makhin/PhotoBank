using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using FluentAssertions;
using Microsoft.Azure.CognitiveServices.Vision.Face;
using Microsoft.Azure.CognitiveServices.Vision.Face.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Rest;
using Minio;
using Moq;
using Moq.Language.Flow;
using NUnit.Framework;
using PhotoBank.Repositories;
using PhotoBank.Services;
using DbFace = PhotoBank.DbContext.Models.Face;
using DbPerson = PhotoBank.DbContext.Models.Person;
using DbPersonFace = PhotoBank.DbContext.Models.PersonFace;
using DbPhoto = PhotoBank.DbContext.Models.Photo;

namespace PhotoBank.UnitTests;

[TestFixture]
public class FaceServiceErrorHandlingTests
{
    private Mock<IRepository<DbFace>> _faceRepository = null!;
    private Mock<IRepository<DbPerson>> _personRepository = null!;
    private Mock<IRepository<DbPersonFace>> _personFaceRepository = null!;
    private Mock<IRepository<DbPhoto>> _photoRepository = null!;
    private Mock<IMinioClient> _minioClient = null!;
    private Mock<IFaceClient> _faceClient = null!;
    private Mock<IFaceOperations> _faceOperations = null!;
    private Mock<IMapper> _mapper = null!;
    private Mock<ILogger<FaceService>> _logger = null!;
    private FaceService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _faceRepository = new Mock<IRepository<DbFace>>();
        _personRepository = new Mock<IRepository<DbPerson>>();
        _personFaceRepository = new Mock<IRepository<DbPersonFace>>();
        _photoRepository = new Mock<IRepository<DbPhoto>>();
        _minioClient = new Mock<IMinioClient>();
        _faceOperations = new Mock<IFaceOperations>();
        _faceClient = new Mock<IFaceClient>();
        _faceClient.SetupGet(x => x.Face).Returns(_faceOperations.Object);
        _mapper = new Mock<IMapper>();
        _logger = new Mock<ILogger<FaceService>>();

        _service = new FaceService(
            _faceClient.Object,
            _faceRepository.Object,
            _personRepository.Object,
            _personFaceRepository.Object,
            _photoRepository.Object,
            _minioClient.Object,
            _mapper.Object,
            _logger.Object);
    }

    [Test]
    public async Task DetectFacesAsync_WhenApiReturnsNull_ShouldReturnEmptyList()
    {
        SetupDetectCall()
            .ReturnsAsync(new HttpOperationResponse<IList<DetectedFace>>
            {
                Body = null!
            });

        var result = await _service.DetectFacesAsync(new byte[] { 1, 2, 3 });

        result.Should().NotBeNull().And.BeEmpty();
        VerifyDetectCalled(Times.Once());
    }

    [Test]
    public void DetectFacesAsync_WhenApiReturnsClientError_ShouldThrowHttpOperationException()
    {
        var exception = CreateHttpException(HttpStatusCode.BadRequest);

        SetupDetectCall()
            .ThrowsAsync(exception);

        Func<Task> act = () => _service.DetectFacesAsync(new byte[] { 4, 5, 6 });

        act.Should().ThrowAsync<HttpOperationException>()
            .WithMessage(exception.Message);
    }

    [Test]
    public void DetectFacesAsync_WhenApiReturnsServerError_ShouldThrowHttpOperationException()
    {
        var exception = CreateHttpException(HttpStatusCode.ServiceUnavailable);

        SetupDetectCall()
            .ThrowsAsync(exception);

        Func<Task> act = () => _service.DetectFacesAsync(new byte[] { 7, 8, 9 });

        act.Should().ThrowAsync<HttpOperationException>()
            .WithMessage(exception.Message);
    }

    [Test]
    public async Task FaceIdentityAsync_WhenDetectionTimeouts_ShouldReturnNullAndSkipIdentify()
    {
        SetupDetectCall()
            .ThrowsAsync(new TaskCanceledException("timeout"));

        var result = await _service.FaceIdentityAsync(new byte[] { 10, 11 });

        result.Should().BeNull();
        _faceOperations.Invocations
            .Where(invocation => invocation.Method.Name == nameof(IFaceOperations.IdentifyWithHttpMessagesAsync))
            .Should().BeEmpty();
    }

    [Test]
    public void FaceIdentityAsync_WhenIdentifyFailsWithServerError_ShouldBubbleException()
    {
        var faceId = Guid.NewGuid();
        var exception = CreateHttpException(HttpStatusCode.InternalServerError);

        SetupDetectCall()
            .ReturnsAsync(new HttpOperationResponse<IList<DetectedFace>>
            {
                Body = new List<DetectedFace> { new() { FaceId = faceId } }
            });

        _faceOperations
            .Setup(x => x.IdentifyWithHttpMessagesAsync(
                It.IsAny<IList<Guid?>>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<int?>(),
                It.IsAny<double?>(),
                It.IsAny<Dictionary<string, List<string>>>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        Func<Task> act = () => _service.FaceIdentityAsync(new byte[] { 12, 13, 14 });

        act.Should().ThrowAsync<HttpOperationException>()
            .WithMessage(exception.Message);
    }

    private ISetup<IFaceOperations, Task<HttpOperationResponse<IList<DetectedFace>>>> SetupDetectCall()
    {
        return _faceOperations
            .Setup(x => x.DetectWithStreamWithHttpMessagesAsync(
                It.IsAny<Stream>(),
                It.IsAny<bool?>(),
                It.IsAny<bool?>(),
                It.IsAny<IList<FaceAttributeType?>>(),
                It.IsAny<string?>(),
                It.IsAny<bool?>(),
                It.IsAny<string?>(),
                It.IsAny<Dictionary<string, List<string>>>(),
                It.IsAny<CancellationToken>()));
    }

    private void VerifyDetectCalled(Times times)
    {
        _faceOperations.Verify(x => x.DetectWithStreamWithHttpMessagesAsync(
                It.IsAny<Stream>(),
                It.IsAny<bool?>(),
                It.IsAny<bool?>(),
                It.IsAny<IList<FaceAttributeType?>>(),
                It.IsAny<string?>(),
                It.IsAny<bool?>(),
                It.IsAny<string?>(),
                It.IsAny<Dictionary<string, List<string>>>(),
                It.IsAny<CancellationToken>()),
            times);
    }

    private static HttpOperationException CreateHttpException(HttpStatusCode statusCode)
    {
        var response = new HttpResponseMessageWrapper(new HttpResponseMessage(statusCode), string.Empty);
        return new HttpOperationException($"Status code {(int)statusCode}")
        {
            Response = response
        };
    }
}
