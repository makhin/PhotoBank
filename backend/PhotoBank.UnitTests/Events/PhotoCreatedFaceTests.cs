using System;
using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using PhotoBank.Services.Events;

namespace PhotoBank.UnitTests.Events;

[TestFixture]
public class PhotoCreatedFaceTests
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    [Test]
    public void Serialize_ShouldProduceCamelCasedBase64Payload()
    {
        var image = new byte[] { 1, 2, 3 };
        var face = new PhotoCreatedFace(42, image);

        var json = JsonSerializer.Serialize(face, SerializerOptions);

        json.Should().Contain("\"faceId\":42");
        json.Should().Contain($"\"image\":\"{Convert.ToBase64String(image)}\"");
    }

    [Test]
    public void Deserialize_ShouldRestoreInstanceWithAllData()
    {
        var image = new byte[] { 10, 20, 30 };
        var json = $"{{\"faceId\":7,\"image\":\"{Convert.ToBase64String(image)}\"}}";

        var result = DeserializeStrict(json);

        result.FaceId.Should().Be(7);
        result.Image.Should().Equal(image);
    }

    [Test]
    public void Deserialize_ShouldFail_WhenFaceIdMissing()
    {
        var image = Convert.ToBase64String(new byte[] { 5 });
        var json = $"{{\"image\":\"{image}\"}}";

        var act = () => DeserializeStrict(json);

        act.Should().Throw<JsonException>().WithMessage("*faceId*");
    }

    [Test]
    public void Deserialize_ShouldFail_WhenImageMissing()
    {
        const string json = "{\"faceId\":13}";

        var act = () => DeserializeStrict(json);

        act.Should().Throw<JsonException>().WithMessage("*image*");
    }

    [Test]
    public void Deserialize_ShouldFail_WhenImageIsNull()
    {
        const string json = "{\"faceId\":99,\"image\":null}";

        var act = () => DeserializeStrict(json);

        act.Should().Throw<JsonException>().WithMessage("*image*");
    }

    private static PhotoCreatedFace DeserializeStrict(string json)
    {
        var result = JsonSerializer.Deserialize<PhotoCreatedFace>(json, SerializerOptions);

        if (result is null)
        {
            throw new JsonException("PhotoCreatedFace payload is empty.");
        }

        if (result.FaceId == default)
        {
            throw new JsonException("PhotoCreatedFace.faceId is required.");
        }

        if (result.Image is null || result.Image.Length == 0)
        {
            throw new JsonException("PhotoCreatedFace.image is required.");
        }

        return result;
    }
}
