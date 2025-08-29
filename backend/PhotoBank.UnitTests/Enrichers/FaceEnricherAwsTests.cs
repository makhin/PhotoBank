using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Rekognition.Model;
using FluentAssertions;
using ImageMagick;
using Moq;
using NUnit.Framework;
using PhotoBank.DbContext.Models;
using PhotoBank.Repositories;
using PhotoBank.Services;
using PhotoBank.Services.Enrichers;
using PhotoBank.Services.Models;
using PhotoBank.UnitTests;
using Minio;
using Minio.DataModel.Args;
using Minio.DataModel.Response;
using Person = PhotoBank.DbContext.Models.Person;

namespace PhotoBank.UnitTests.Enrichers
{
    [TestFixture]
    public class FaceEnricherAwsTests
    {
        private Mock<IFaceServiceAws> _mockFaceService;
        private Mock<IRepository<Person>> _mockPersonRepository;
        private Mock<IMinioClient> _mockMinio;
        private FaceEnricherAws _faceEnricher;
        private List<Person> _persons;

        [SetUp]
        public void Setup()
        {
            _mockFaceService = new Mock<IFaceServiceAws>();
            _mockPersonRepository = new Mock<IRepository<Person>>();
            _mockMinio = new Mock<IMinioClient>();
            _persons = new List<Person>
            {
                new Person { Id = 1, Name = "John Doe", ExternalGuid = Guid.NewGuid() },
                new Person { Id = 2, Name = "Jane Doe", ExternalGuid = Guid.NewGuid() }
            };
            _mockPersonRepository.Setup(repo => repo.GetAll()).Returns(_persons.AsQueryable());
            _mockMinio.Setup(m => m.PutObjectAsync(It.IsAny<PutObjectArgs>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PutObjectResponse(System.Net.HttpStatusCode.OK, "bucket", new System.Collections.Generic.Dictionary<string, string>(), 0, "etag"));
            _faceEnricher = new FaceEnricherAws(_mockFaceService.Object, _mockPersonRepository.Object, _mockMinio.Object);
        }

        [Test]
        public void EnricherType_ShouldReturnFace()
        {
            // Act
            var result = _faceEnricher.EnricherType;

            // Assert
            result.Should().Be(EnricherType.Face);
        }

        [Test]
        public void Dependencies_ShouldReturnPreviewAndMetadataEnricher()
        {
            // Act
            var result = _faceEnricher.Dependencies;

            // Assert
            result.Should().Contain(new[] { typeof(PreviewEnricher), typeof(MetadataEnricher) });
        }

        [Test]
        public async Task EnrichAsync_ShouldSetFaceIdentifyStatusToNotDetected_WhenNoFacesDetected()
        {
            // Arrange
            var photo = new Photo();
            var preview1 = new MagickImage(MagickColors.Red, 10, 10) { Format = MagickFormat.Jpeg };
            var sourceData = new SourceDataDto { PreviewImage = preview1 };
            _mockFaceService.Setup(service => service.DetectFacesAsync(It.IsAny<byte[]>()))
                .ReturnsAsync(new List<FaceDetail>());

            // Act
            await _faceEnricher.EnrichAsync(photo, sourceData);

            // Assert
            photo.FaceIdentifyStatus.Should().Be(FaceIdentifyStatus.NotDetected);
        }

        [Test]
        public async Task EnrichAsync_ShouldSetFaceIdentifyStatusToDetected_WhenFacesDetected()
        {
            // Arrange
            var photo = new Photo();
            var preview2 = new MagickImage(MagickColors.Red, 10, 10) { Format = MagickFormat.Jpeg };
            var sourceData = new SourceDataDto { PreviewImage = preview2 };
            var detectedFaces = new List<FaceDetail>
            {
                new FaceDetail { BoundingBox = new BoundingBox { Height = 0.1f, Width = 0.1f, Top = 0.1f, Left = 0.1f } }
            };
            _mockFaceService.Setup(service => service.DetectFacesAsync(It.IsAny<byte[]>()))
                .ReturnsAsync(detectedFaces);

            // Act
            await _faceEnricher.EnrichAsync(photo, sourceData);

            // Assert
            photo.FaceIdentifyStatus.Should().Be(FaceIdentifyStatus.Detected);
        }

        [RaspberrySkipFact]
        [Ignore("ImageMagick delegates not available in test environment")]
        public async Task EnrichAsync_ShouldAddFacesToPhoto_WhenFacesDetected()
        {
            // Arrange
            var photo = new Photo();
            var preview3 = new MagickImage(MagickColors.Red, 100, 100) { Format = MagickFormat.Jpeg };
            var sourceData = new SourceDataDto
            {
                PreviewImage = preview3
            };
            var detectedFaces = new List<FaceDetail>
            {
                new FaceDetail { BoundingBox = new BoundingBox { Height = 0.1f, Width = 0.1f, Top = 0.1f, Left = 0.1f } }
            };
            _mockFaceService.Setup(service => service.DetectFacesAsync(It.IsAny<byte[]>()))
                .ReturnsAsync(detectedFaces);

            // Act
            await _faceEnricher.EnrichAsync(photo, sourceData);

            // Assert
            photo.Faces.Should().HaveCount(1);
        }

        [RaspberrySkipFact]
        [Ignore("ImageMagick delegates not available in test environment")]
        public async Task EnrichAsync_ShouldIdentifyFaces_WhenFacesDetected()
        {
            // Arrange
            var photo = new Photo();
            var preview4 = new MagickImage(MagickColors.Red, 100, 100) { Format = MagickFormat.Jpeg };
            var sourceData = new SourceDataDto
            {
                PreviewImage = preview4
            };
            var detectedFaces = new List<FaceDetail>
            {
                new FaceDetail { BoundingBox = new BoundingBox { Height = 0.1f, Width = 0.1f, Top = 0.1f, Left = 0.1f } }
            };
            var userMatches = new List<UserMatch>
            {
                new UserMatch
                {
                    User = new MatchedUser { UserId = _persons[0].Id.ToString() },
                    Similarity = 0.9f
                }
            };
            _mockFaceService.Setup(service => service.DetectFacesAsync(It.IsAny<byte[]>()))
                .ReturnsAsync(detectedFaces);
            _mockFaceService.Setup(service => service.SearchUsersByImageAsync(It.IsAny<byte[]>()))
                .ReturnsAsync(userMatches);

            // Act
            await _faceEnricher.EnrichAsync(photo, sourceData);

            // Assert
            photo.Faces.Should().HaveCount(1);
            photo.Faces[0].IdentityStatus.Should().Be(IdentityStatus.Identified);
            photo.Faces[0].IdentifiedWithConfidence.Should().Be(0.9f);
            photo.Faces[0].Person.Should().Be(_persons[0]);
            photo.Faces[0].S3Key_Image.Should().NotBeNull();
        }
    }
}

