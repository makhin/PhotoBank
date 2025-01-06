using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using PhotoBank.DbContext.Models;
using PhotoBank.Repositories;
using PhotoBank.Services.Enrichers;
using PhotoBank.Services.Enrichers.Services;
using Microsoft.Azure.CognitiveServices.Vision.Face.Models;
using Person = PhotoBank.DbContext.Models.Person;
using PhotoBank.Services;
using ImageMagick;
using PhotoBank.Services.Models;

namespace PhotoBank.UnitTests.Enrichers
{
    [TestFixture]
    public class FaceEnricherTests
    {
        private Mock<IFaceService> _mockFaceService;
        private Mock<IRepository<Person>> _mockPersonRepository;
        private Mock<IFacePreviewService> _mockFacePreviewService;
        private FaceEnricher _faceEnricher;
        private List<Person> _persons;

        [SetUp]
        public void Setup()
        {
            _mockFaceService = new Mock<IFaceService>();
            _mockPersonRepository = new Mock<IRepository<Person>>();
            _mockFacePreviewService = new Mock<IFacePreviewService>();
            _persons = new List<Person>
            {
                new Person { Id = 1, Name = "John Doe", ExternalGuid = Guid.NewGuid() },
                new Person { Id = 2, Name = "Jane Doe", ExternalGuid = Guid.NewGuid() }
            };
            _mockPersonRepository.Setup(repo => repo.GetAll()).Returns(_persons.AsQueryable());
            _faceEnricher = new FaceEnricher(_mockFaceService.Object, _mockPersonRepository.Object, _mockFacePreviewService.Object);
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
            var sourceData = new SourceDataDto();
            _mockFaceService.Setup(service => service.DetectFacesAsync(It.IsAny<byte[]>()))
                .ReturnsAsync(new List<DetectedFace>());

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
            var sourceData = new SourceDataDto();
            var detectedFaces = new List<DetectedFace>
            {
                new DetectedFace { FaceId = Guid.NewGuid() }
            };
            _mockFaceService.Setup(service => service.DetectFacesAsync(It.IsAny<byte[]>()))
                .ReturnsAsync(detectedFaces);

            // Act
            await _faceEnricher.EnrichAsync(photo, sourceData);

            // Assert
            photo.FaceIdentifyStatus.Should().Be(FaceIdentifyStatus.Detected);
        }

        [Test]
        public async Task EnrichAsync_ShouldIdentifyFacesAndAddFacesToPhoto_WhenFacesDetected()
        {
            // Arrange
            var photo = new Photo();
            var sourceData = new SourceDataDto();
            var detectedFaces = new List<DetectedFace>
            {
                new DetectedFace { FaceId = Guid.NewGuid(), FaceRectangle = new FaceRectangle { Height = 50, Width = 50, Top = 10, Left = 10 } }
            };
            var identifyResults = new List<IdentifyResult>
            {
                new IdentifyResult
                {
                    FaceId = detectedFaces[0].FaceId.Value,
                    Candidates = new List<IdentifyCandidate>
                    {
                        new IdentifyCandidate { Confidence = 0.9, PersonId = _persons[0].ExternalGuid }
                    }
                }
            };
            _mockFaceService.Setup(service => service.DetectFacesAsync(It.IsAny<byte[]>()))
                .ReturnsAsync(detectedFaces);
            _mockFaceService.Setup(service => service.IdentifyAsync(It.IsAny<IList<Guid?>>()))
                .ReturnsAsync(identifyResults);
            _mockFacePreviewService.Setup(service => service.CreateFacePreview(It.IsAny<DetectedFace>(), It.IsAny<IMagickImage<byte>>(), It.IsAny<double>()))
                .ReturnsAsync(new byte[] { 1, 2, 3 });

            // Act
            await _faceEnricher.EnrichAsync(photo, sourceData);

            // Assert
            photo.Faces.Should().HaveCount(1);
            photo.Faces[0].IdentityStatus.Should().Be(IdentityStatus.Identified);
            photo.Faces[0].IdentifiedWithConfidence.Should().Be(0.9);
            photo.Faces[0].Person.Should().Be(_persons[0]);
        }
    }
}

