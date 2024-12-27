using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using MetadataExtractor.Formats.FileSystem;
using Moq;
using NUnit.Framework;
using PhotoBank.DbContext.Models;
using PhotoBank.Dto.Load;
using PhotoBank.Services.Enrichers;
using PhotoBank.Services.Enrichers.Services;
using Directory = MetadataExtractor.Directory;
using File = PhotoBank.DbContext.Models.File;

namespace PhotoBank.UnitTests.Enrichers
{
    [TestFixture]
    public class MetadataEnricherTests
    {
        private MetadataEnricher _metadataEnricher;
        private Photo _photo;
        private SourceDataDto _sourceData;
        private Mock<IImageMetadataReaderWrapper> _mockImageMetadataReaderWrapper;

        [SetUp]
        public void Setup()
        {
            _mockImageMetadataReaderWrapper = new Mock<IImageMetadataReaderWrapper>();
            _metadataEnricher = new MetadataEnricher(_mockImageMetadataReaderWrapper.Object);

            _photo = new Photo { Storage = new Storage { Folder = "c:\\storageFolder" } };
            _sourceData = new SourceDataDto { AbsolutePath = "c:\\storageFolder\\folder\\photo.jpg" };
        }

        [Test]
        public void EnricherType_ShouldReturnMetadata()
        {
            // Act
            var result = _metadataEnricher.EnricherType;

            // Assert
            result.Should().Be(EnricherType.Metadata);
        }

        [Test]
        public void Dependencies_ShouldReturnPreviewEnricher()
        {
            // Act
            var result = _metadataEnricher.Dependencies;

            // Assert
            result.Should().ContainSingle()
                .And.Contain(typeof(PreviewEnricher));
        }

        [Test]
        public async Task EnrichAsync_ShouldSetPhotoProperties()
        {
            // Arrange
            var directories = new List<Directory>
            {
                new ExifIfd0Directory(),
                new ExifSubIfdDirectory(),
                new FileMetadataDirectory(),
                new GpsDirectory()
            };

            _mockImageMetadataReaderWrapper.Setup(reader => reader.ReadMetadata(It.IsAny<string>()))
                .Returns(directories);

            // Act
            await _metadataEnricher.EnrichAsync(_photo, _sourceData);

            // Assert
            _photo.Name.Should().Be("photo");
            _photo.RelativePath.Should().Be("folder");
            _photo.Files.Should().ContainSingle()
                .Which.Name.Should().Be("photo.jpg");
        }

        [Test]
        public async Task GetTakenDate_ShouldReturnCorrectDate()
        {
            // Arrange
            var directory = new ExifIfd0Directory();
            directory.Set(ExifDirectoryBase.TagDateTime, "2021:01:01 12:00:00");

            _mockImageMetadataReaderWrapper.Setup(reader => reader.ReadMetadata(It.IsAny<string>())).Returns(new List<Directory> { directory });

            // Act
            await _metadataEnricher.EnrichAsync(_photo, _sourceData);

            // Assert            
            _photo.TakenDate.Should().Be(new DateTime(2021, 1, 1, 12, 0, 0));
        }

        [Test]
        public async Task GetHeight_ShouldReturnCorrectHeight()
        {
            // Arrange
            var directory = new ExifSubIfdDirectory();
            directory.Set(ExifDirectoryBase.TagImageHeight, 1080);

            _mockImageMetadataReaderWrapper.Setup(reader => reader.ReadMetadata(It.IsAny<string>())).Returns(new List<Directory> { directory });

            // Act
            await _metadataEnricher.EnrichAsync(_photo, _sourceData);

            // Assert
            _photo.Height.Should().Be(1080);
        }

        [Test]
        public async Task GetWidth_ShouldReturnCorrectWidth()
        {
            // Arrange
            var directory = new ExifSubIfdDirectory();
            directory.Set(ExifDirectoryBase.TagImageWidth, 1920);

            _mockImageMetadataReaderWrapper.Setup(reader => reader.ReadMetadata(It.IsAny<string>())).Returns(new List<Directory> { directory });

            // Act
            await _metadataEnricher.EnrichAsync(_photo, _sourceData);

            // Assert
            _photo.Width.Should().Be(1920);
        }

        [Test]
        public async Task GetOrientation_ShouldReturnCorrectOrientation()
        {
            // Arrange
            var directory = new ExifIfd0Directory();
            directory.Set(ExifDirectoryBase.TagOrientation, 1);

            _mockImageMetadataReaderWrapper.Setup(reader => reader.ReadMetadata(It.IsAny<string>())).Returns(new List<Directory> { directory });

            // Act
            await _metadataEnricher.EnrichAsync(_photo, _sourceData);

            // Assert
            _photo.Orientation.Should().Be(1);
        }
    }
}
