using FluentAssertions;
using NetTopologySuite.Geometries;
using NUnit.Framework;
using PhotoBank.DbContext.Models;
using PhotoBank.Services;

namespace PhotoBank.UnitTests;

[TestFixture]
public class FaceHelperTests
{
    [Test]
    public void GetFaceBox_ReturnsScaledFaceBox()
    {
        var coordinates = new[]
        {
            new Coordinate(10, 20),
            new Coordinate(30, 20),
            new Coordinate(30, 40),
            new Coordinate(10, 40),
            new Coordinate(10, 20)
        };
        var geometry = new GeometryFactory().CreatePolygon(coordinates);
        var photo = new Photo { Scale = 2 };

        var result = FaceHelper.GetFaceBox(geometry, photo);

        result.Left.Should().Be(20);
        result.Top.Should().Be(40);
        result.Width.Should().Be(40);
        result.Height.Should().Be(40);
    }

    [TestCase(null)]
    [TestCase("")]
    public void GetFriendlyFaceAttributes_ReturnsNotAvailable_ForNullOrEmpty(string? attributes)
    {
        var result = FaceHelper.GetFriendlyFaceAttributes(attributes);

        result.Should().Be("Not available");
    }

    [Test]
    public void GetFriendlyFaceAttributes_ParsesAzureJson()
    {
        var json = "{\"age\":30}";

        var result = FaceHelper.GetFriendlyFaceAttributes(json);

        result.Should().Contain("Age : 30");
    }

    [Test]
    public void GetFriendlyFaceAttributes_ParsesAwsJson()
    {
        var json = "{\"AgeRange\":{\"Low\":15,\"High\":20}}";

        var result = FaceHelper.GetFriendlyFaceAttributes(json);

        result.Should().Contain("between 15 and 20 years old");
    }
}

