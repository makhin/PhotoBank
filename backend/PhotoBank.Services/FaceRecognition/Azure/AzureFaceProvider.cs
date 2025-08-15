using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.CognitiveServices.Vision.Face;
using Microsoft.Azure.CognitiveServices.Vision.Face.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PhotoBank.Services.FaceRecognition.Abstractions;

namespace PhotoBank.Services.FaceRecognition.Azure;

public sealed class AzureFaceProvider : IFaceProvider
{
    public FaceProviderKind Kind => FaceProviderKind.Azure;

    private readonly IFaceClient _client;
    private readonly AzureFaceOptions _opts;
    private readonly ILogger<AzureFaceProvider> _log;

    private static readonly IList<FaceAttributeType?> DefaultAttrs = new List<FaceAttributeType?>
    {
        FaceAttributeType.Age, FaceAttributeType.Gender, FaceAttributeType.Emotion, FaceAttributeType.Glasses
    };

    public AzureFaceProvider(IFaceClient client, IOptions<AzureFaceOptions> opts, ILogger<AzureFaceProvider> log)
    {
        _client = client;
        _opts = opts.Value;
        _log = log;
    }

    public async Task EnsureReadyAsync(CancellationToken ct)
    {
        try
        {
            await _client.PersonGroup.GetAsync(_opts.PersonGroupId, cancellationToken: ct);
        }
        catch
        {
            await _client.PersonGroup.CreateAsync(_opts.PersonGroupId, _opts.PersonGroupId, recognitionModel: _opts.RecognitionModel, cancellationToken: ct);
            _log.LogInformation("Created Azure PersonGroup {Group}", _opts.PersonGroupId);
        }
    }

    public async Task<IReadOnlyDictionary<int, string>> UpsertPersonsAsync(IReadOnlyCollection<PersonSyncItem> persons, CancellationToken ct)
    {
        var servicePersons = await _client.PersonGroupPerson.ListAsync(_opts.PersonGroupId, cancellationToken: ct);
        // будем искать по userData == PersonId.ToString()
        var byUserData = servicePersons.Where(p => int.TryParse(p.UserData, out _)).ToDictionary(p => p.UserData!, p => p.PersonId);

        var map = new Dictionary<int, string>(persons.Count);
        foreach (var p in persons)
        {
            var key = p.PersonId.ToString();
            if (!byUserData.TryGetValue(key, out var azureId))
            {
                var created = await _client.PersonGroupPerson.CreateAsync(_opts.PersonGroupId, p.Name, userData: key, cancellationToken: ct);
                azureId = created.PersonId;
                _log.LogDebug("Create Azure person {AzureId} userData={UserData}", azureId, key);
            }
            map[p.PersonId] = azureId.ToString();
        }
        return map;
    }

    public async Task<IReadOnlyDictionary<int, string>> LinkFacesToPersonAsync(int personId, IReadOnlyCollection<FaceToLink> faces, CancellationToken ct)
    {
        // Найти azure person по userData
        var people = await _client.PersonGroupPerson.ListAsync(_opts.PersonGroupId, cancellationToken: ct);
        var person = people.FirstOrDefault(x => x.UserData == personId.ToString());
        if (person is null)
        {
            var created = await _client.PersonGroupPerson.CreateAsync(_opts.PersonGroupId, name: $"Person {personId}", userData: personId.ToString(), cancellationToken: ct);
            person = created;
        }

        // Текущее состояние лиц у персоны
        var current = await _client.PersonGroupPerson.GetAsync(_opts.PersonGroupId, person.PersonId, cancellationToken: ct);
        var existing = (current.PersistedFaceIds ?? new List<Guid?>()).Where(x => x.HasValue).Select(x => x!.Value).ToHashSet();

        var result = new Dictionary<int, string>(faces.Count);
        foreach (var f in faces)
        {
            using var stream = f.OpenStream();
            var added = await _client.PersonGroupPerson.AddFaceFromStreamAsync(
                _opts.PersonGroupId, person.PersonId, stream, userData: f.FaceId.ToString(), cancellationToken: ct);
            if (added?.PersistedFaceId is Guid g)
            {
                existing.Add(g);
                result[f.FaceId] = g.ToString();
            }
        }

        await _client.PersonGroup.TrainAsync(_opts.PersonGroupId, cancellationToken: ct);
        await WaitTrainingAsync(ct);

        return result;
    }

    public async Task<IReadOnlyList<DetectedFaceDto>> DetectAsync(Stream image, CancellationToken ct)
    {
        image.Position = 0;
        var faces = await _client.Face.DetectWithStreamAsync(
            image,
            recognitionModel: _opts.RecognitionModel,
            detectionModel: _opts.DetectionModel,
            returnFaceId: true,
            returnFaceLandmarks: false,
            returnFaceAttributes: _opts.DetectionModel.Equals("detection_02", StringComparison.OrdinalIgnoreCase) ? null : DefaultAttrs,
            cancellationToken: ct);

        return faces?.Select(f => new DetectedFaceDto(
            ProviderFaceId: f.FaceId?.ToString() ?? "",
            Confidence: null,
            Age: (float?)f.FaceAttributes?.Age,
            Gender: f.FaceAttributes?.Gender?.ToString())).ToList() ?? [];
    }

    public async Task<IReadOnlyList<IdentifyResultDto>> IdentifyAsync(IReadOnlyList<string> providerFaceIds, CancellationToken ct)
    {
        // ожидаем GUIDы от Detect/внешних вызовов
        var ids = providerFaceIds.Select(s => Guid.TryParse(s, out var g) ? g : (Guid?)null)
                                 .Where(g => g.HasValue).Select(g => g!.Value).ToList();
        if (ids.Count == 0) return Array.Empty<IdentifyResultDto>();

        var results = new List<IdentifyResultDto>(ids.Count);
        var chunk = Math.Max(1, _opts.IdentifyChunkSize);
        foreach (var part in ids.Chunk(chunk))
        {
            var r = await _client.Face.IdentifyAsync(part.Select(g => (Guid?)g).ToList(), _opts.PersonGroupId, cancellationToken: ct);
            results.AddRange(r.Select(x => new IdentifyResultDto(
                ProviderFaceId: x.FaceId.ToString(),
                Candidates: x.Candidates.Select(c => new IdentifyCandidateDto(c.PersonId.ToString(), (float)c.Confidence)).ToList()
            )));
        }
        return results;
    }

    public Task<IReadOnlyList<UserMatchDto>> SearchUsersByImageAsync(Stream image, CancellationToken ct)
        => Task.FromResult<IReadOnlyList<UserMatchDto>>(Array.Empty<UserMatchDto>());

    private async Task<bool> WaitTrainingAsync(CancellationToken ct)
    {
        var start = DateTime.UtcNow;
        while (true)
        {
            var s = await _client.PersonGroup.GetTrainingStatusAsync(_opts.PersonGroupId, cancellationToken: ct);
            if (s.Status is TrainingStatusType.Succeeded) return true;
            if (s.Status is TrainingStatusType.Failed)
            {
                _log.LogError("Azure training failed: {Message}", s.Message);
                return false;
            }
            if ((DateTime.UtcNow - start).TotalSeconds > _opts.TrainTimeoutSeconds)
            {
                _log.LogWarning("Azure training timeout");
                return false;
            }
            await Task.Delay(1000, ct);
        }
    }
}

