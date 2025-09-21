using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Azure.CognitiveServices.Vision.Face;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using PhotoBank.Services.FaceRecognition.Abstractions;
using PhotoBank.Services.FaceRecognition.Azure;

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
        var handler = new SequenceHandler();
        RequestSnapshot? detectedRequest = null;
        handler.Enqueue((req, _) =>
        {
            detectedRequest = RequestSnapshot.From(req);
            var json = "[" +
                       "{\"faceId\":\"11111111-1111-1111-1111-111111111111\",\"faceAttributes\":{\"age\":25.4,\"gender\":\"male\"}}," +
                       "{\"faceId\":\"22222222-2222-2222-2222-222222222222\",\"faceAttributes\":{\"age\":32.1,\"gender\":\"female\"}}" +
                       "]";
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });
        });
        var logger = new TestLogger<AzureFaceProvider>();
        var provider = CreateProvider(options, handler, logger);

        using var image = new MemoryStream(new byte[] { 1, 2, 3 });
        var result = await provider.DetectAsync(image, CancellationToken.None);

        detectedRequest.Should().NotBeNull();
        detectedRequest!.Method.Should().Be(HttpMethod.Post);
        detectedRequest.Path.Should().Be("/face/v1.0/detect");
        detectedRequest.Headers.Should().ContainKey("Ocp-Apim-Subscription-Key");
        detectedRequest.Headers["Ocp-Apim-Subscription-Key"].Should().Contain(ApiKey);

        var query = ParseQuery(detectedRequest.Query);
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
        var handler = new SequenceHandler();
        RequestSnapshot? detectedRequest = null;
        handler.Enqueue((req, _) =>
        {
            detectedRequest = RequestSnapshot.From(req);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("[{\"faceId\":\"33333333-3333-3333-3333-333333333333\"}]", Encoding.UTF8, "application/json")
            });
        });
        var logger = new TestLogger<AzureFaceProvider>();
        var provider = CreateProvider(options, handler, logger);

        using var image = new MemoryStream(new byte[] { 1 });
        var faces = await provider.DetectAsync(image, CancellationToken.None);

        detectedRequest.Should().NotBeNull();
        var query = ParseQuery(detectedRequest!.Query);
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
        var handler = new SequenceHandler();
        RequestSnapshot? getRequest = null;
        RequestSnapshot? createRequest = null;
        handler.Enqueue((req, _) =>
        {
            getRequest = RequestSnapshot.From(req);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent("{\"error\":{\"code\":\"PersonGroupNotFound\",\"message\":\"not found\"}}", Encoding.UTF8, "application/json")
            });
        });
        handler.Enqueue((req, _) =>
        {
            createRequest = RequestSnapshot.From(req);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
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
        var handler = new SequenceHandler();
        RequestSnapshot? addFaceRequest = null;
        RequestSnapshot? trainRequest = null;
        handler.Enqueue((req, _) => // list persons
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("[]", Encoding.UTF8, "application/json")
            });
        });
        handler.Enqueue((req, _) => // create person
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"personId\":\"11111111-1111-1111-1111-111111111111\"}", Encoding.UTF8, "application/json")
            });
        });
        handler.Enqueue((req, _) => // get person
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"persistedFaceIds\":[]}", Encoding.UTF8, "application/json")
            });
        });
        handler.Enqueue((req, _) => // add face
        {
            addFaceRequest = RequestSnapshot.From(req);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"persistedFaceId\":\"22222222-2222-2222-2222-222222222222\"}", Encoding.UTF8, "application/json")
            });
        });
        handler.Enqueue((req, _) => // train
        {
            trainRequest = RequestSnapshot.From(req);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Accepted));
        });
        handler.Enqueue((req, _) => // training status
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"status\":\"failed\",\"message\":\"boom\"}", Encoding.UTF8, "application/json")
            });
        });
        var logger = new TestLogger<AzureFaceProvider>();
        var provider = CreateProvider(options, handler, logger);

        var faces = new[] { new FaceToLink(42, () => new MemoryStream(new byte[] { 5, 4, 3 }), null) };
        var result = await provider.LinkFacesToPersonAsync(10, faces, CancellationToken.None);

        addFaceRequest.Should().NotBeNull();
        addFaceRequest!.Method.Should().Be(HttpMethod.Post);
        addFaceRequest.Path.Should().Be("/face/v1.0/persongroups/group-1/persons/11111111-1111-1111-1111-111111111111/persistedfaces");
        var addQuery = ParseQuery(addFaceRequest.Query);
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
        var handler = new SequenceHandler();
        handler.Enqueue((req, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("[{\"personId\":\"11111111-1111-1111-1111-111111111111\",\"userData\":\"10\"}]", Encoding.UTF8, "application/json")
        }));
        handler.Enqueue((req, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"persistedFaceIds\":[]}", Encoding.UTF8, "application/json")
        }));
        handler.Enqueue((req, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"persistedFaceId\":\"22222222-2222-2222-2222-222222222222\"}", Encoding.UTF8, "application/json")
        }));
        handler.Enqueue((req, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.Accepted)));
        handler.Enqueue((req, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"status\":\"running\"}", Encoding.UTF8, "application/json")
        }));

        var logger = new TestLogger<AzureFaceProvider>();
        var provider = CreateProvider(options, handler, logger);

        var faces = new[] { new FaceToLink(42, () => new MemoryStream(new byte[] { 5, 4, 3 }), null) };
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
        var handler = new SequenceHandler();
        RequestSnapshot? createRequest = null;
        handler.Enqueue((req, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("[{\"personId\":\"aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa\",\"userData\":\"1\"}]", Encoding.UTF8, "application/json")
        }));
        handler.Enqueue((req, _) =>
        {
            createRequest = RequestSnapshot.From(req);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"personId\":\"bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb\"}", Encoding.UTF8, "application/json")
            });
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
        var handler = new SequenceHandler();
        var firstRequestBodies = new List<string>();
        handler.Enqueue(async (req, _) =>
        {
            firstRequestBodies.Add(await req.Content!.ReadAsStringAsync());
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("[" +
                    "{\"faceId\":\"11111111-1111-1111-1111-111111111111\",\"candidates\":[{\"personId\":\"aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa\",\"confidence\":0.9}]} ," +
                    "{\"faceId\":\"22222222-2222-2222-2222-222222222222\",\"candidates\":[]}" +
                    "]", Encoding.UTF8, "application/json")
            };
        });
        handler.Enqueue(async (req, _) =>
        {
            firstRequestBodies.Add(await req.Content!.ReadAsStringAsync());
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("[{\"faceId\":\"33333333-3333-3333-3333-333333333333\",\"candidates\":[{\"personId\":\"bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb\",\"confidence\":0.42}]}]", Encoding.UTF8, "application/json")
            };
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
        var handler = new SequenceHandler();
        var logger = new TestLogger<AzureFaceProvider>();
        var provider = CreateProvider(options, handler, logger);

        var result = await provider.IdentifyAsync(new[] { "not-a-guid" }, CancellationToken.None);

        result.Should().BeEmpty();
        handler.PendingHandlers.Should().Be(0);
    }

    private static AzureFaceProvider CreateProvider(AzureFaceOptions options, SequenceHandler handler, ILogger<AzureFaceProvider> logger)
    {
        var client = new FaceClient(new ApiKeyServiceClientCredentials(options.Key), new HttpClient(handler), true)
        {
            Endpoint = options.Endpoint
        };
        return new AzureFaceProvider(client, Options.Create(options), logger);
    }

    private static IReadOnlyDictionary<string, List<string>> ParseQuery(string query)
    {
        if (string.IsNullOrEmpty(query))
        {
            return new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        }

        return query.TrimStart('?')
            .Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Select(part => part.Split('=', 2))
            .GroupBy(parts => Uri.UnescapeDataString(parts[0]), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key,
                          g => g.Select(parts => parts.Length > 1 ? Uri.UnescapeDataString(parts[1]) : string.Empty).ToList(),
                          StringComparer.OrdinalIgnoreCase);
    }

    private sealed class SequenceHandler : HttpMessageHandler
    {
        private readonly Queue<Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>> _queue = new();

        public void Enqueue(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
            => _queue.Enqueue(handler);

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (_queue.Count == 0)
            {
                throw new InvalidOperationException($"Unexpected request: {request.Method} {request.RequestUri}");
            }

            return _queue.Dequeue().Invoke(request, cancellationToken);
        }

        public int PendingHandlers => _queue.Count;
    }

    private sealed record RequestSnapshot(HttpMethod Method, string Path, string Query, Dictionary<string, string[]> Headers, string? Body)
    {
        public static RequestSnapshot From(HttpRequestMessage request)
        {
            var headers = request.Headers.ToDictionary(h => h.Key, h => h.Value.ToArray(), StringComparer.OrdinalIgnoreCase);
            string? body = null;
            if (request.Content != null)
            {
                body = request.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            }
            return new RequestSnapshot(request.Method, request.RequestUri!.AbsolutePath, request.RequestUri!.Query, headers, body);
        }
    }

    private sealed class TestLogger<T> : ILogger<T>
    {
        public List<LogEntry> Entries { get; } = new();

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            Entries.Add(new LogEntry(logLevel, formatter(state, exception), exception));
        }
    }

    private sealed record LogEntry(LogLevel Level, string Message, Exception? Exception);

    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();

        public void Dispose()
        {
        }
    }
}
