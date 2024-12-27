using System;
using System.Collections.Generic;
using Amazon.Rekognition.Model;
using FluentAssertions;
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using NetTopologySuite.Geometries;
using NUnit.Framework;
using PhotoBank.Services;
using Geometry = NetTopologySuite.Geometries.Geometry;
using Point = NetTopologySuite.Geometries.Point;

namespace PhotoBank.UnitTests.Services
{
    [TestFixture]
    public class GeoWrapperTests
    {
        [Test]
        public void GetRectangleArray_ShouldReturnCorrectArray()
        {
            // Arrange
            var geometry = GeoWrapper.GetRectangle(100, 100, new BoundingBox { Left = 0.1f, Top = 0.1f, Width = 0.8f, Height = 0.8f });

            // Act
            var result = GeoWrapper.GetRectangleArray(geometry);

            // Assert
            result.Should().Equal(new List<int?> { 0, 0, 80, 80 });
        }

        [Test]
        public void GetRectangle_WithBoundingBox_ShouldReturnCorrectGeometry()
        {
            // Arrange
            var boundingBox = new BoundingBox { Left = 0.1f, Top = 0.1f, Width = 0.8f, Height = 0.8f };

            // Act
            var result = GeoWrapper.GetRectangle(100, 100, boundingBox);

            // Assert
            result.Should().BeOfType<Polygon>();
            result.Coordinates.Should().HaveCount(5);
            result.Coordinates[0].Should().Be(new Coordinate(10, 10));
            result.Coordinates[1].Should().Be(new Coordinate(90, 10));
            result.Coordinates[2].Should().Be(new Coordinate(90, 90));
            result.Coordinates[3].Should().Be(new Coordinate(10, 90));
            result.Coordinates[4].Should().Be(new Coordinate(10, 10));
        }

        [Test]
        public void GetRectangle_WithBoundingRect_ShouldReturnCorrectGeometry()
        {
            // Arrange
            var boundingRect = new BoundingRect { X = 10, Y = 10, W = 80, H = 80 };

            // Act
            var result = GeoWrapper.GetRectangle(boundingRect);

            // Assert
            result.Should().BeOfType<Polygon>();
            result.Coordinates.Should().HaveCount(5);
            result.Coordinates[0].Should().Be(new Coordinate(10, 10));
            result.Coordinates[1].Should().Be(new Coordinate(90, 10));
            result.Coordinates[2].Should().Be(new Coordinate(90, 90));
            result.Coordinates[3].Should().Be(new Coordinate(10, 90));
            result.Coordinates[4].Should().Be(new Coordinate(10, 10));
        }

        [Test]
        public void GetRectangle_WithFaceRectangle_ShouldReturnCorrectGeometry()
        {
            // Arrange
            var faceRectangle = new FaceRectangle { Left = 10, Top = 10, Width = 80, Height = 80 };

            // Act
            var result = GeoWrapper.GetRectangle(faceRectangle);

            // Assert
            result.Should().BeOfType<Polygon>();
            result.Coordinates.Should().HaveCount(5);
            result.Coordinates[0].Should().Be(new Coordinate(10, 10));
            result.Coordinates[1].Should().Be(new Coordinate(90, 10));
            result.Coordinates[2].Should().Be(new Coordinate(90, 90));
            result.Coordinates[3].Should().Be(new Coordinate(10, 90));
            result.Coordinates[4].Should().Be(new Coordinate(10, 10));
        }

        [Test]
        public void GetRectangle_WithAzureFaceRectangle_ShouldReturnCorrectGeometry()
        {
            // Arrange
            var azureFaceRectangle = new Microsoft.Azure.CognitiveServices.Vision.Face.Models.FaceRectangle { Left = 10, Top = 10, Width = 80, Height = 80 };

            // Act
            var result = GeoWrapper.GetRectangle(azureFaceRectangle, 1);

            // Assert
            result.Should().BeOfType<Polygon>();
            result.Coordinates.Should().HaveCount(5);
            result.Coordinates[0].Should().Be(new Coordinate(10, 10));
            result.Coordinates[1].Should().Be(new Coordinate(90, 10));
            result.Coordinates[2].Should().Be(new Coordinate(90, 90));
            result.Coordinates[3].Should().Be(new Coordinate(10, 90));
            result.Coordinates[4].Should().Be(new Coordinate(10, 10));
        }

        [Test]
        [Ignore("Need to fix")]
        public void GetLocation_ShouldReturnCorrectPoint()
        {
            // Arrange
            var gpsDirectory = new GpsDirectory();

            gpsDirectory.Set(GpsDirectory.TagLatitude, new[] { 40.0, 0.0, 0.0 });
            gpsDirectory.Set(GpsDirectory.TagLatitudeRef, "N");
            gpsDirectory.Set(GpsDirectory.TagLongitude, new[] { 70.0, 0.0, 0.0 });
            gpsDirectory.Set(GpsDirectory.TagLongitudeRef, "W");

            // Act
            var result = GeoWrapper.GetLocation(gpsDirectory);

            // Assert
            result.Should().BeOfType<Point>();
            result.Coordinate.Should().Be(new Coordinate(40.0, -70.0));
        }
    }
}

