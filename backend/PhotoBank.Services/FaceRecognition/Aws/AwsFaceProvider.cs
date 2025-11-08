using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Rekognition;
using Amazon.Rekognition.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PhotoBank.Services.FaceRecognition.Abstractions;

namespace PhotoBank.Services.FaceRecognition.Aws;

public sealed class AwsFaceProvider : IFaceProvider
{
    public FaceProviderKind Kind => FaceProviderKind.Aws;

    private readonly AmazonRekognitionClient _client;
    private readonly RekognitionOptions _opts;
    private readonly ILogger<AwsFaceProvider> _log;

    public AwsFaceProvider(AmazonRekognitionClient client, IOptions<RekognitionOptions> opts, ILogger<AwsFaceProvider> log)
    {
        _client = client;
        _opts = opts.Value;
        _log = log;
    }

    public async Task EnsureReadyAsync(CancellationToken ct)
    {
        var list = await _client.ListCollectionsAsync(new ListCollectionsRequest { MaxResults = 100 }, ct);
        if (!list.CollectionIds.Contains(_opts.CollectionId))
        {
            await _client.CreateCollectionAsync(new CreateCollectionRequest { CollectionId = _opts.CollectionId }, ct);
            _log.LogInformation("Created Rekognition collection {Collection}", _opts.CollectionId);
        }
    }

    public async Task<IReadOnlyDictionary<int, string>> UpsertPersonsAsync(IReadOnlyCollection<PersonSyncItem> persons, CancellationToken ct)
    {
        var existing = new HashSet<string>();
        string token = null;
        do
        {
            var resp = await _client.ListUsersAsync(new ListUsersRequest
            {
                CollectionId = _opts.CollectionId,
                MaxResults = 500,
                NextToken = token
            }, ct);
            foreach (var u in resp.Users) existing.Add(u.UserId);
            token = resp.NextToken;
        } while (!string.IsNullOrEmpty(token));

        var map = new Dictionary<int, string>(persons.Count);
        foreach (var p in persons)
        {
            var uid = p.PersonId.ToString();
            if (!existing.Contains(uid))
            {
                await _client.CreateUserAsync(new CreateUserRequest
                {
                    CollectionId = _opts.CollectionId,
                    UserId = uid
                }, ct);
                existing.Add(uid);
                _log.LogDebug("CreateUser {UserId}", uid);
            }
            map[p.PersonId] = uid; // внешний ID = UserId (string)
        }
        return map;
    }

    public async Task<IReadOnlyDictionary<int, string>> LinkFacesToPersonAsync(int personId, IReadOnlyCollection<FaceToLink> faces, CancellationToken ct)
    {
        // Получаем уже привязанные FaceIds
        var existing = new HashSet<string>();
        string token = null;
        do
        {
            var resp = await _client.ListFacesAsync(new ListFacesRequest
            {
                CollectionId = _opts.CollectionId,
                UserId = personId.ToString(),
                MaxResults = 500,
                NextToken = token
            }, ct);
            foreach (var f in resp.Faces) existing.Add(f.FaceId);
            token = resp.NextToken;
        } while (!string.IsNullOrEmpty(token));

        var result = new ConcurrentDictionary<int, string>();
        using var sem = new SemaphoreSlim(_opts.MaxParallelism);
        var tasks = faces.Select(async f =>
        {
            await sem.WaitAsync(ct);
            try
            {
                using var stream = f.OpenStream();
                using var ms = new MemoryStream();
                await stream.CopyToAsync(ms, ct);
                ms.Position = 0;
                // 1) index
                var index = await _client.IndexFacesAsync(new IndexFacesRequest
                {
                    CollectionId = _opts.CollectionId,
                    MaxFaces = 1,
                    Image = new Image { Bytes = ms },
                    DetectionAttributes = new List<string> { "ALL" },
                    QualityFilter = _opts.QualityFilter
                }, ct);

                var newId = index.FaceRecords.FirstOrDefault()?.Face?.FaceId;
                if (string.IsNullOrEmpty(newId)) return;

                // 2) associate, если ещё нет
                if (!existing.Contains(newId))
                {
                    await _client.AssociateFacesAsync(new AssociateFacesRequest
                    {
                        CollectionId = _opts.CollectionId,
                        UserId = personId.ToString(),
                        FaceIds = new List<string> { newId }
                    }, ct);
                    existing.Add(newId);
                    _log.LogDebug("Associate Face {FaceId} -> User {UserId}", newId, personId);
                }
                result[f.FaceId] = newId!;
            }
            catch (Exception ex)
            {
                _log.LogWarning(ex, "Failed to link face {FaceId} to user {UserId}", f.FaceId, personId);
            }
            finally { sem.Release(); }
        });

        await Task.WhenAll(tasks);
        return result;
    }

    public async Task<IReadOnlyList<DetectedFaceDto>> DetectAsync(Stream image, CancellationToken ct)
    {
        image.Position = 0;
        using var ms = new MemoryStream();
        await image.CopyToAsync(ms, ct);
        ms.Position = 0;
        var resp = await _client.DetectFacesAsync(new DetectFacesRequest
        {
            Image = new Image { Bytes = ms },
            Attributes = new List<string> { "ALL" }
        }, ct);

        // У DetectFaces нет глобального FaceId — оставим пустой
        return resp.FaceDetails?.Select(fd =>
            new DetectedFaceDto(
                ProviderFaceId: "",
                Confidence: fd.Confidence,
                Age: (fd.AgeRange?.Low + fd.AgeRange?.High) / 2f,
                Gender: fd.Gender?.Value,
                BoundingBox: fd.BoundingBox != null
                    ? new FaceBoundingBox(
                        Left: fd.BoundingBox.Left,
                        Top: fd.BoundingBox.Top,
                        Width: fd.BoundingBox.Width,
                        Height: fd.BoundingBox.Height)
                    : null))
            .ToList() ?? new List<DetectedFaceDto>();
    }

    public Task<IReadOnlyList<IdentifyResultDto>> IdentifyAsync(IReadOnlyList<string> providerFaceIds, CancellationToken ct)
        => Task.FromResult<IReadOnlyList<IdentifyResultDto>>(Array.Empty<IdentifyResultDto>());

    public async Task<IReadOnlyList<UserMatchDto>> SearchUsersByImageAsync(Stream image, CancellationToken ct)
    {
        image.Position = 0;
        using var ms = new MemoryStream();
        await image.CopyToAsync(ms, ct);
        ms.Position = 0;
        var resp = await _client.SearchUsersByImageAsync(new SearchUsersByImageRequest
        {
            CollectionId = _opts.CollectionId,
            Image = new Image { Bytes = ms },
            MaxUsers = 10,
            UserMatchThreshold = _opts.FaceMatchThreshold,
            QualityFilter = _opts.QualityFilter
        }, ct);

        return resp.UserMatches?.Select(m => new UserMatchDto(m.User?.UserId ?? string.Empty, m.Similarity ?? 0f)).ToList() ?? new List<UserMatchDto>();
    }
}
