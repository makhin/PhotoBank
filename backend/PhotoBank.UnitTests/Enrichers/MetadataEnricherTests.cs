using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using MetadataExtractor.Formats.Exif;
using MetadataExtractor.Formats.FileSystem;
using Moq;
using NUnit.Framework;
using PhotoBank.DbContext.Models;
using PhotoBank.Services.Enrichers;
using PhotoBank.Services.Enrichers.Services;
using PhotoBank.Services.Models;
using Directory = MetadataExtractor.Directory;

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
        public void Dependencies_ShouldReturnDuplicateEnricher()
        {
            // Act
            var result = _metadataEnricher.Dependencies;

            // Assert
            result.Should().ContainSingle()
                .And.Contain(typeof(DuplicateEnricher));
        }

        [Test]
        public async Task EnrichAsync_ShouldCallMetadataReader()
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
            // Name, RelativePath, and Files creation moved to DuplicateEnricher
            // This enricher now only reads EXIF metadata
            _mockImageMetadataReaderWrapper.Verify(
                reader => reader.ReadMetadata(_sourceData.AbsolutePath),
                Times.Once);
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
    }
}
