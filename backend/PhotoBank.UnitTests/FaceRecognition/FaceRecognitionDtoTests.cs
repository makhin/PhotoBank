using System.Collections.Generic;
using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using PhotoBank.Services.FaceRecognition.Abstractions;
using PhotoBank.Services.FaceRecognition.Local;

namespace PhotoBank.Tests.FaceRecognition;

[TestFixture]
public class IdentifyDtoTests
{
    [Test]
    public void IdentifyCandidateDto_ShouldSupportValueEquality()
    {
        var candidate1 = new IdentifyCandidateDto("person-1", 0.9f);
        var candidate2 = new IdentifyCandidateDto("person-1", 0.9f);

        candidate1.Should().Be(candidate2);
        candidate1.GetHashCode().Should().Be(candidate2.GetHashCode());
    }

    [Test]
    public void IdentifyCandidateDto_ShouldRoundtripThroughJson()
    {
        var candidate = new IdentifyCandidateDto("person-1", 0.9f);

        var json = JsonSerializer.Serialize(candidate);
        var deserialized = JsonSerializer.Deserialize<IdentifyCandidateDto>(json);

        deserialized.Should().NotBeNull();
        deserialized.Should().Be(candidate);
    }

    [Test]
    public void IdentifyResultDto_ShouldRoundtripWithCandidates()
    {
        var candidate = new IdentifyCandidateDto("person-1", 0.9f);
        var result = new IdentifyResultDto("face-1", new[] { candidate });

        var json = JsonSerializer.Serialize(result);
        var deserialized = JsonSerializer.Deserialize<IdentifyResultDto>(json);

        deserialized.Should().NotBeNull();
        deserialized!.ProviderFaceId.Should().Be(result.ProviderFaceId);
        deserialized.Candidates.Should().BeEquivalentTo(result.Candidates);
    }

    [Test]
    public void IdentifyResultDto_ShouldExposeNonNullCandidates()
    {
        var result = new IdentifyResultDto("face-1", new List<IdentifyCandidateDto>());

        result.Candidates.Should().NotBeNull();
    }

    [Test]
    public void IdentifyResultDto_ShouldSupportValueEquality()
    {
        var candidates = new List<IdentifyCandidateDto> { new("person-1", 0.9f) };

        var result1 = new IdentifyResultDto("face-1", candidates);
        var result2 = new IdentifyResultDto("face-1", candidates);

        result1.Should().Be(result2);
        result1.GetHashCode().Should().Be(result2.GetHashCode());
    }
}

[TestFixture]
public class LocalDetectionDtoTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    [Test]
    public void LocalDetectedFace_ShouldSupportValueEquality()
    {
        var bbox = new[] { 1f, 2f, 3f, 4f };
        var landmark = new[] { new[] { 5f, 6f }, new[] { 7f, 8f }, new[] { 9f, 10f } };

        var face1 = new LocalDetectedFace("face-1", 0.82f, bbox, landmark, 30, "male");
        var face2 = new LocalDetectedFace("face-1", 0.82f, bbox, landmark, 30, "male");

        face1.Should().Be(face2);
        face1.GetHashCode().Should().Be(face2.GetHashCode());
    }

    [Test]
    public void LocalDetectedFace_ShouldRoundtripThroughJson()
    {
        var face = new LocalDetectedFace(
            "face-1",
            0.82f,
            new[] { 1f, 2f, 3f, 4f },
            new[] { new[] { 5f, 6f }, new[] { 7f, 8f }, new[] { 9f, 10f } },
            30,
            "male");

        var json = JsonSerializer.Serialize(face, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<LocalDetectedFace>(json, JsonOptions);

        deserialized.Should().NotBeNull();
        deserialized!.Should().BeEquivalentTo(face);
    }

    [Test]
    public void LocalDetectedFace_ShouldAllowNullOptionalMembers()
    {
        const string json = "{\"id\":\"face-1\",\"score\":0.5,\"bbox\":null,\"landmark\":null,\"age\":null,\"gender\":null}";

        var deserialized = JsonSerializer.Deserialize<LocalDetectedFace>(json, JsonOptions);

        deserialized.Should().NotBeNull();
        deserialized!.Bbox.Should().BeNull();
        deserialized.Landmark.Should().BeNull();
        deserialized.Age.Should().BeNull();
        deserialized.Gender.Should().BeNull();
    }

    [Test]
    public void LocalDetectResponse_ShouldRoundtripThroughJson()
    {
        var face = new LocalDetectedFace("face-1", 0.82f, new[] { 1f, 2f, 3f, 4f }, null, 30, "male");
        var faces = new List<LocalDetectedFace> { face };
        var response = new LocalDetectResponse(faces);

        var json = JsonSerializer.Serialize(response, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<LocalDetectResponse>(json, JsonOptions);

        deserialized.Should().NotBeNull();
        deserialized!.Faces.Should().BeEquivalentTo(response.Faces);
    }

    [Test]
    public void LocalDetectResponse_ShouldSupportValueEquality()
    {
        var faces = new List<LocalDetectedFace>
        {
            new("face-1", 0.82f, new[] { 1f, 2f, 3f, 4f }, null, 30, "male")
        };

        var response1 = new LocalDetectResponse(faces);
        var response2 = new LocalDetectResponse(faces);

        response1.Should().Be(response2);
        response1.GetHashCode().Should().Be(response2.GetHashCode());
    }
}
