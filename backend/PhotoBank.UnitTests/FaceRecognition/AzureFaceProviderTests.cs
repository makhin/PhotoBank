using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using ImageMagick;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using PhotoBank.Services.FaceRecognition.Abstractions;
using PhotoBank.Services.FaceRecognition.Azure;
using PhotoBank.UnitTests.Infrastructure.FaceRecognition;
using PhotoBank.UnitTests.Infrastructure.Http;
using PhotoBank.UnitTests.Infrastructure.Logging;

namespace PhotoBank.UnitTests.FaceRecognition;

[TestFixture]
public class AzureFaceProviderTests
{
    private const string Endpoint = "https://example.com";
    private const string ApiKey = "test-key";

    [Test]
    public async Task DetectAsync_SendsConfiguredRequestAndParsesResponse()
    {
        var options = new AzureFaceOptions
        {
            Endpoint = Endpoint,
            Key = ApiKey,
            DetectionModel = "detection_01",
            RecognitionModel = "recognition_04"
        };
        var handler = new HttpMockSequenceHandler();
        HttpRequestSnapshot? detectedRequest = null;
        handler.Enqueue((req, _) =>
        {
            detectedRequest = HttpRequestSnapshot.Capture(req);
            var response = HttpResponseBuilder.Create()
                .WithJson(new[]
                {
                    new
                    {
                        faceId = "11111111-1111-1111-1111-111111111111",
                        faceAttributes = new { age = 25.4, gender = "male" }
                    },
                    new
                    {
                        faceId = "22222222-2222-2222-2222-222222222222",
                        faceAttributes = new { age = 32.1, gender = "female" }
                    }
                })
                .Build();
            return Task.FromResult(response);
        });
        var logger = new TestLogger<AzureFaceProvider>();
        var provider = CreateProvider(options, handler, logger);

        using var image = await CreateTestImageAsync();
        var result = await provider.DetectAsync(image, CancellationToken.None);

        detectedRequest.Should().NotBeNull();
        detectedRequest!.Method.Should().Be(HttpMethod.Post);
        detectedRequest.Path.Should().Be("/face/v1.0/detect");
        detectedRequest.Headers.Should().ContainKey("Ocp-Apim-Subscription-Key");
        detectedRequest.Headers["Ocp-Apim-Subscription-Key"].Should().Contain(ApiKey);

        var query = QueryStringParser.Parse(detectedRequest.Query);
        query.Should().ContainKey("recognitionModel");
        query["recognitionModel"].Should().ContainSingle().Which.Should().Be("recognition_04");
        query.Should().ContainKey("detectionModel");
        query["detectionModel"].Should().ContainSingle().Which.Should().Be("detection_01");
        query.Should().ContainKey("returnFaceId");
        query["returnFaceId"].Should().ContainSingle().Which.Should().Be("true");
        query.Should().ContainKey("returnFaceLandmarks");
        query["returnFaceLandmarks"].Should().ContainSingle().Which.Should().Be("false");
        query.Should().ContainKey("returnRecognitionModel");
        query["returnRecognitionModel"].Should().ContainSingle().Which.Should().Be("false");
        query.Should().ContainKey("returnFaceAttributes");
        var attributes = query["returnFaceAttributes"]
            .SelectMany(v => v.Split(',', StringSplitOptions.RemoveEmptyEntries))
            .Select(v => v.Trim())
            .Where(v => v.Length > 0)
            .Select(v => v.ToLowerInvariant())
            .ToList();
        attributes.Should().BeEquivalentTo(new[] { "age", "gender", "emotion", "glasses" });

        result.Should().HaveCount(2);
        result[0].ProviderFaceId.Should().Be("11111111-1111-1111-1111-111111111111");
        result[0].Age.Should().BeApproximately(25.4f, 0.001f);
        result[0].Gender.Should().Be("Male", "Gender should be propagated as string");
        result[1].ProviderFaceId.Should().Be("22222222-2222-2222-2222-222222222222");
        result[1].Age.Should().BeApproximately(32.1f, 0.001f);
        result[1].Gender.Should().Be("Female");
    }

    [Test]
    public async Task DetectAsync_Detection02_OmitsAttributesParameter()
    {
        var options = new AzureFaceOptions
        {
            Endpoint = Endpoint,
            Key = ApiKey,
            DetectionModel = "detection_02",
            RecognitionModel = "recognition_04"
        };
        var handler = new HttpMockSequenceHandler();
        HttpRequestSnapshot? detectedRequest = null;
        handler.Enqueue((req, _) =>
        {
            detectedRequest = HttpRequestSnapshot.Capture(req);
            var response = HttpResponseBuilder.Create()
                .WithJson(new[]
                {
                    new { faceId = "33333333-3333-3333-3333-333333333333" }
                })
                .Build();
            return Task.FromResult(response);
        });
        var logger = new TestLogger<AzureFaceProvider>();
        var provider = CreateProvider(options, handler, logger);

        using var image = await CreateTestImageAsync();
        var faces = await provider.DetectAsync(image, CancellationToken.None);

        detectedRequest.Should().NotBeNull();
        var query = QueryStringParser.Parse(detectedRequest!.Query);
        query.Should().NotContainKey("returnFaceAttributes");
        faces.Should().ContainSingle(f => f.ProviderFaceId == "33333333-3333-3333-3333-333333333333");
    }

    [Test]
    public async Task EnsureReadyAsync_CreatesGroupWhenMissing()
    {
        var options = new AzureFaceOptions
        {
            Endpoint = Endpoint,
            Key = ApiKey,
            PersonGroupId = "group-1",
            RecognitionModel = "recognition_03"
        };
        var handler = new HttpMockSequenceHandler();
        HttpRequestSnapshot? getRequest = null;
        HttpRequestSnapshot? createRequest = null;
        handler.Enqueue((req, _) =>
        {
            getRequest = HttpRequestSnapshot.Capture(req);
            var response = HttpResponseBuilder.Create()
                .WithStatus(HttpStatusCode.NotFound)
                .WithError("PersonGroupNotFound", "not found")
                .Build();
            return Task.FromResult(response);
        });
        handler.Enqueue((req, _) =>
        {
            createRequest = HttpRequestSnapshot.Capture(req);
            return Task.FromResult(HttpResponseBuilder.Create().Build());
        });
        var logger = new TestLogger<AzureFaceProvider>();
        var provider = CreateProvider(options, handler, logger);

        await provider.EnsureReadyAsync(CancellationToken.None);

        getRequest.Should().NotBeNull();
        getRequest!.Method.Should().Be(HttpMethod.Get);
        getRequest.Path.Should().Be("/face/v1.0/persongroups/group-1");

        createRequest.Should().NotBeNull();
        createRequest!.Method.Should().Be(HttpMethod.Put);
        createRequest.Path.Should().Be("/face/v1.0/persongroups/group-1");
        createRequest.Body.Should().NotBeNull();
        using (var doc = JsonDocument.Parse(createRequest.Body!))
        {
            doc.RootElement.GetProperty("recognitionModel").GetString().Should().Be("recognition_03");
        }

        logger.Entries.Should().Contain(e => e.Level == LogLevel.Information && e.Message.Contains("Created Azure PersonGroup group-1", StringComparison.Ordinal));
    }

    [Test]
    public async Task LinkFacesToPersonAsync_AddsFacesAndLogsTrainingFailure()
    {
        var options = new AzureFaceOptions
        {
            Endpoint = Endpoint,
            Key = ApiKey,
            PersonGroupId = "group-1",
            TrainTimeoutSeconds = 30
        };
        var handler = new HttpMockSequenceHandler();
        HttpRequestSnapshot? addFaceRequest = null;
        HttpRequestSnapshot? trainRequest = null;
        handler.Enqueue((req, _) => // list persons
        {
            var response = HttpResponseBuilder.Create()
                .WithJson(Array.Empty<object>())
                .Build();
            return Task.FromResult(response);
        });
        handler.Enqueue((req, _) => // create person
        {
            var response = HttpResponseBuilder.Create()
                .WithJson(new { personId = "11111111-1111-1111-1111-111111111111" })
                .Build();
            return Task.FromResult(response);
        });
        handler.Enqueue((req, _) => // get person
        {
            var response = HttpResponseBuilder.Create()
                .WithJson(new { persistedFaceIds = Array.Empty<string>() })
                .Build();
            return Task.FromResult(response);
        });
        handler.Enqueue((req, _) => // add face
        {
            addFaceRequest = HttpRequestSnapshot.Capture(req);
            var response = HttpResponseBuilder.Create()
                .WithJson(new { persistedFaceId = "22222222-2222-2222-2222-222222222222" })
                .Build();
            return Task.FromResult(response);
        });
        handler.Enqueue((req, _) => // train
        {
            trainRequest = HttpRequestSnapshot.Capture(req);
            return Task.FromResult(HttpResponseBuilder.Create().WithStatus(HttpStatusCode.Accepted).Build());
        });
        handler.Enqueue((req, _) => // training status
        {
            var response = HttpResponseBuilder.Create()
                .WithJson(new { status = "failed", message = "boom" })
                .Build();
            return Task.FromResult(response);
        });
        var logger = new TestLogger<AzureFaceProvider>();
        var provider = CreateProvider(options, handler, logger);

        using var testImage = await CreateTestImageAsync();
        var faces = new[] { new FaceToLink(42, () => testImage, null) };
        var result = await provider.LinkFacesToPersonAsync(10, faces, CancellationToken.None);

        addFaceRequest.Should().NotBeNull();
        addFaceRequest!.Method.Should().Be(HttpMethod.Post);
        addFaceRequest.Path.Should().Be("/face/v1.0/persongroups/group-1/persons/11111111-1111-1111-1111-111111111111/persistedfaces");
        var addQuery = QueryStringParser.Parse(addFaceRequest.Query);
        addQuery.Should().ContainKey("userData");
        addQuery["userData"].Should().ContainSingle().Which.Should().Be("42");

        trainRequest.Should().NotBeNull();
        trainRequest!.Method.Should().Be(HttpMethod.Post);
        trainRequest.Path.Should().Be("/face/v1.0/persongroups/group-1/train");

        result.Should().ContainKey(42);
        result[42].Should().Be("22222222-2222-2222-2222-222222222222");
        logger.Entries.Should().Contain(e => e.Level == LogLevel.Error && e.Message.Contains("Azure training failed: boom", StringComparison.Ordinal));
    }

    [Test]
    public async Task LinkFacesToPersonAsync_LogsTrainingTimeout()
    {
        var options = new AzureFaceOptions
        {
            Endpoint = Endpoint,
            Key = ApiKey,
            PersonGroupId = "group-1",
            TrainTimeoutSeconds = 0
        };
        var handler = new HttpMockSequenceHandler();
        handler.Enqueue((req, _) => Task.FromResult(
            HttpResponseBuilder.Create()
                .WithJson(new[] { new { personId = "11111111-1111-1111-1111-111111111111", userData = "10" } })
                .Build()));
        handler.Enqueue((req, _) => Task.FromResult(
            HttpResponseBuilder.Create()
                .WithJson(new { persistedFaceIds = Array.Empty<string>() })
                .Build()));
        handler.Enqueue((req, _) => Task.FromResult(
            HttpResponseBuilder.Create()
                .WithJson(new { persistedFaceId = "22222222-2222-2222-2222-222222222222" })
                .Build()));
        handler.Enqueue((req, _) => Task.FromResult(
            HttpResponseBuilder.Create().WithStatus(HttpStatusCode.Accepted).Build()));
        handler.Enqueue((req, _) => Task.FromResult(
            HttpResponseBuilder.Create().WithJson(new { status = "running" }).Build()));

        var logger = new TestLogger<AzureFaceProvider>();
        var provider = CreateProvider(options, handler, logger);

        using var testImage = await CreateTestImageAsync();
        var faces = new[] { new FaceToLink(42, () => testImage, null) };
        await provider.LinkFacesToPersonAsync(10, faces, CancellationToken.None);

        logger.Entries.Should().Contain(e => e.Level == LogLevel.Warning && e.Message.Contains("Azure training timeout", StringComparison.Ordinal));
    }

    [Test]
    public async Task UpsertPersonsAsync_UsesPersonUserDataAsKey()
    {
        var options = new AzureFaceOptions
        {
            Endpoint = Endpoint,
            Key = ApiKey,
            PersonGroupId = "group-1"
        };
        var handler = new HttpMockSequenceHandler();
        HttpRequestSnapshot? createRequest = null;
        handler.Enqueue((req, _) => Task.FromResult(
            HttpResponseBuilder.Create()
                .WithJson(new[] { new { personId = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa", userData = "1" } })
                .Build()));
        handler.Enqueue((req, _) =>
        {
            createRequest = HttpRequestSnapshot.Capture(req);
            var response = HttpResponseBuilder.Create()
                .WithJson(new { personId = "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb" })
                .Build();
            return Task.FromResult(response);
        });
        var logger = new TestLogger<AzureFaceProvider>();
        var provider = CreateProvider(options, handler, logger);

        var persons = new[]
        {
            new PersonSyncItem(1, "Alice", null),
            new PersonSyncItem(2, "Bob", null)
        };
        var result = await provider.UpsertPersonsAsync(persons, CancellationToken.None);

        result.Should().HaveCount(2);
        result[1].Should().Be("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        result[2].Should().Be("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

        createRequest.Should().NotBeNull();
        createRequest!.Method.Should().Be(HttpMethod.Post);
        createRequest.Path.Should().Be("/face/v1.0/persongroups/group-1/persons");
        var body = JsonDocument.Parse(createRequest.Body ?? "{}");
        body.RootElement.GetProperty("userData").GetString().Should().Be("2");
        body.RootElement.GetProperty("name").GetString().Should().Be("Bob");
    }

    [Test]
    public async Task IdentifyAsync_SendsBatchedRequestsAndParsesCandidates()
    {
        var options = new AzureFaceOptions
        {
            Endpoint = Endpoint,
            Key = ApiKey,
            PersonGroupId = "group-1",
            IdentifyChunkSize = 2
        };
        var handler = new HttpMockSequenceHandler();
        var firstRequestBodies = new List<string>();
        handler.Enqueue(async (req, _) =>
        {
            firstRequestBodies.Add(await req.Content!.ReadAsStringAsync());
            var response = HttpResponseBuilder.Create()
                .WithJson(new[]
                {
                    new
                    {
                        faceId = "11111111-1111-1111-1111-111111111111",
                        candidates = new object[] { new { personId = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa", confidence = 0.9 } }
                    },
                    new
                    {
                        faceId = "22222222-2222-2222-2222-222222222222",
                        candidates = Array.Empty<object>()
                    }
                })
                .Build();
            return response;
        });
        handler.Enqueue(async (req, _) =>
        {
            firstRequestBodies.Add(await req.Content!.ReadAsStringAsync());
            var response = HttpResponseBuilder.Create()
                .WithJson(new[]
                {
                    new
                    {
                        faceId = "33333333-3333-3333-3333-333333333333",
                        candidates = new object[] { new { personId = "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb", confidence = 0.42 } }
                    }
                })
                .Build();
            return response;
        });
        var logger = new TestLogger<AzureFaceProvider>();
        var provider = CreateProvider(options, handler, logger);

        var ids = new[]
        {
            "11111111-1111-1111-1111-111111111111",
            "22222222-2222-2222-2222-222222222222",
            "33333333-3333-3333-3333-333333333333"
        };

        var result = await provider.IdentifyAsync(ids, CancellationToken.None);

        firstRequestBodies.Should().HaveCount(2);
        var firstPayload = JsonDocument.Parse(firstRequestBodies[0]);
        firstPayload.RootElement.GetProperty("faceIds").EnumerateArray().Select(x => x.GetString()).Should().BeEquivalentTo(ids.Take(2));
        var secondPayload = JsonDocument.Parse(firstRequestBodies[1]);
        secondPayload.RootElement.GetProperty("faceIds").EnumerateArray().Select(x => x.GetString()).Should().BeEquivalentTo(ids.Skip(2));

        result.Should().HaveCount(3);
        result[0].ProviderFaceId.Should().Be(ids[0]);
        result[0].Candidates.Should().ContainSingle(c => c.ProviderPersonId == "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa" && Math.Abs(c.Confidence - 0.9f) < 0.0001f);
        result[1].Candidates.Should().BeEmpty();
        result[2].Candidates.Should().ContainSingle(c => c.ProviderPersonId == "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    }

    [Test]
    public async Task IdentifyAsync_InvalidIds_ReturnsEmptyAndDoesNotCallApi()
    {
        var options = new AzureFaceOptions
        {
            Endpoint = Endpoint,
            Key = ApiKey,
            PersonGroupId = "group-1"
        };
        var handler = new HttpMockSequenceHandler();
        var logger = new TestLogger<AzureFaceProvider>();
        var provider = CreateProvider(options, handler, logger);

        var result = await provider.IdentifyAsync(new[] { "not-a-guid" }, CancellationToken.None);

        result.Should().BeEmpty();
        handler.PendingHandlers.Should().Be(0);
    }

    [Test]
    public async Task DetectAsync_NormalizesBoundingBoxCoordinates()
    {
        // Azure returns absolute pixel coordinates, we need to verify they're normalized to 0-1 range
        var options = new AzureFaceOptions
        {
            Endpoint = Endpoint,
            Key = ApiKey,
            DetectionModel = "detection_01",
            RecognitionModel = "recognition_04"
        };
        var handler = new HttpMockSequenceHandler();
        handler.Enqueue((req, _) =>
        {
            var response = HttpResponseBuilder.Create()
                .WithJson(new[]
                {
                    new
                    {
                        faceId = "11111111-1111-1111-1111-111111111111",
                        faceRectangle = new { left = 100, top = 200, width = 300, height = 400 },
                        faceAttributes = new { age = 25.0 }
                    }
                })
                .Build();
            return Task.FromResult(response);
        });
        var logger = new TestLogger<AzureFaceProvider>();
        var provider = CreateProvider(options, handler, logger);

        // Create a real image (1000x1000) to test normalization
        using var image = new MemoryStream();
        using (var magickImage = new MagickImage(MagickColors.Red, 1000, 1000))
        {
            magickImage.Format = MagickFormat.Jpeg;
            magickImage.Quality = 10; // Low quality to reduce size
            await magickImage.WriteAsync(image);
        }
        image.Position = 0;

        var result = await provider.DetectAsync(image, CancellationToken.None);

        result.Should().ContainSingle();
        var face = result[0];

        // Azure returned absolute coordinates: (100, 200, 300, 400) on 1000x1000 image
        // Should be normalized to: (0.1, 0.2, 0.3, 0.4)
        face.BoundingBox.Should().NotBeNull();
        face.BoundingBox!.Left.Should().BeApproximately(0.1f, 0.001f, "Left should be normalized: 100/1000 = 0.1");
        face.BoundingBox.Top.Should().BeApproximately(0.2f, 0.001f, "Top should be normalized: 200/1000 = 0.2");
        face.BoundingBox.Width.Should().BeApproximately(0.3f, 0.001f, "Width should be normalized: 300/1000 = 0.3");
        face.BoundingBox.Height.Should().BeApproximately(0.4f, 0.001f, "Height should be normalized: 400/1000 = 0.4");
    }

    private static AzureFaceProvider CreateProvider(AzureFaceOptions options, HttpMockSequenceHandler handler, ILogger<AzureFaceProvider> logger)
    {
        var client = AzureFaceClientFactory.Create(options, handler);
        return new AzureFaceProvider(client, Options.Create(options), logger);
    }

    /// <summary>
    /// Creates a minimal valid JPEG image for testing.
    /// Azure provider needs to read image dimensions using ImageMagick.
    /// </summary>
    private static async Task<MemoryStream> CreateTestImageAsync(uint width = 100, uint height = 100)
    {
        var stream = new MemoryStream();
        using (var image = new MagickImage(MagickColors.Red, width, height))
        {
            image.Format = MagickFormat.Jpeg;
            image.Quality = 1; // Minimum quality for smallest size
            await image.WriteAsync(stream);
        }
        stream.Position = 0;
        return stream;
    }
}
