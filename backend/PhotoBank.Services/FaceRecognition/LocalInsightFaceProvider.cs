using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PhotoBank.Services.FaceRecognition.Abstractions;
using PhotoBank.Services.FaceRecognition.Local;
using static System.Math;

namespace PhotoBank.Services.FaceRecognition;

public sealed class LocalInsightFaceProvider : IFaceProvider
{
    public FaceProviderKind Kind => FaceProviderKind.Local;

    private readonly ILocalInsightFaceClient _client;
    private readonly IFaceEmbeddingRepository _embeddings;
    private readonly LocalInsightFaceOptions _opts;
    private readonly ILogger<LocalInsightFaceProvider> _log;

    public LocalInsightFaceProvider(
        ILocalInsightFaceClient client,
        IFaceEmbeddingRepository embeddings,
        Microsoft.Extensions.Options.IOptions<LocalInsightFaceOptions> opts,
        ILogger<LocalInsightFaceProvider> log)
    {
        _client = client;
        _embeddings = embeddings;
        _opts = opts.Value;
        _log = log;
    }

    public Task EnsureReadyAsync(CancellationToken ct) => Task.CompletedTask;

    public Task<IReadOnlyDictionary<int, string>> UpsertPersonsAsync(IReadOnlyCollection<PersonSyncItem> persons, CancellationToken ct)
        => Task.FromResult((IReadOnlyDictionary<int, string>)persons.ToDictionary(p => p.PersonId, p => $"local:{p.PersonId}"));

    public async Task<IReadOnlyDictionary<int, string>> LinkFacesToPersonAsync(int personId, IReadOnlyCollection<FaceToLink> faces, CancellationToken ct)
    {
        var result = new Dictionary<int, string>(faces.Count);
        using var sem = new SemaphoreSlim(_opts.MaxParallelism);
        var tasks = faces.Select(async f =>
        {
            await sem.WaitAsync(ct);
            try
            {
                using var stream = f.OpenStream();
                var emb = await _client.EmbedAsync(stream, includeAttributes: false, ct);
                await _embeddings.UpsertAsync(personId, f.FaceId, Normalize(emb.Embedding), emb.Model ?? _opts.Model, ct);
                result[f.FaceId] = $"local:{f.FaceId}";
            }
            finally { sem.Release(); }
        });
        await Task.WhenAll(tasks);
        return result;
    }

    public async Task<IReadOnlyList<DetectedFaceDto>> DetectAsync(Stream image, CancellationToken ct)
    {
        var resp = await _client.DetectAsync(image, includeEmbeddings: false, ct);
        return resp.Faces.Select(f => new DetectedFaceDto(
            ProviderFaceId: f.Id ?? string.Empty,
            Confidence: f.Score,
            Age: f.Age,
            Gender: f.Gender,
            BoundingBox: f.Bbox != null && f.Bbox.Length >= 4
                ? new FaceBoundingBox(
                    Left: f.Bbox[0],
                    Top: f.Bbox[1],
                    Width: f.Bbox[2] - f.Bbox[0],
                    Height: f.Bbox[3] - f.Bbox[1])
                : null,
            Emotion: f.Emotion,
            EmotionScores: f.EmotionScores
        )).ToList();
    }

    public Task<IReadOnlyList<IdentifyResultDto>> IdentifyAsync(IReadOnlyList<string> providerFaceIds, CancellationToken ct)
        => Task.FromResult<IReadOnlyList<IdentifyResultDto>>(Array.Empty<IdentifyResultDto>());

    public async Task<IReadOnlyList<UserMatchDto>> SearchUsersByImageAsync(Stream image, CancellationToken ct)
    {
        var embResp = await _client.EmbedAsync(image, includeAttributes: false, ct);
        var q = Normalize(embResp.Embedding);

        // Use optimized pgvector similarity search with HNSW index and SQL-level grouping
        // FindSimilarFacesAsync returns the best face for each person (using DISTINCT ON),
        // already sorted by distance and limited to TopK persons
        var similarFaces = await _embeddings.FindSimilarFacesAsync(q, _opts.TopK, ct);

        // Convert cosine distance to similarity: pgvector CosineDistance = 1 - cosine_similarity
        var results = similarFaces
            .Select(x => new UserMatchDto($"local:{x.PersonId}", 1f - x.Distance))
            .Where(x => x.Confidence >= _opts.FaceMatchThreshold)
            .ToList();

        return results;
    }

    private static float[] Normalize(float[] v)
    {
        var sum = 0f; for (int i = 0; i < v.Length; i++) sum += v[i] * v[i];
        var inv = 1f / (float)Sqrt(Max(sum, 1e-12f));
        var r = new float[v.Length];
        for (int i = 0; i < v.Length; i++) r[i] = v[i] * inv;
        return r;
    }

    private static float CosSim(float[] a, float[] b)
    {
        var len = Math.Min(a.Length, b.Length);
        double dot = 0, na = 0, nb = 0;
        for (int i = 0; i < len; i++) { dot += a[i] * b[i]; na += a[i] * a[i]; nb += b[i] * b[i]; }
        if (na == 0 || nb == 0) return 0;
        return (float)(dot / (Math.Sqrt(na) * Math.Sqrt(nb)));
    }
}

