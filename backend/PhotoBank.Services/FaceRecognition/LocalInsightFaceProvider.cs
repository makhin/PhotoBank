using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PhotoBank.DbContext.Models;
using PhotoBank.Repositories;
using PhotoBank.Services.FaceRecognition.Abstractions;
using PhotoBank.Services.FaceRecognition.Local;
using static System.Math;

namespace PhotoBank.Services.FaceRecognition;

public sealed class LocalInsightFaceProvider : IFaceProvider
{
    public FaceProviderKind Kind => FaceProviderKind.Local;

    private readonly ILocalInsightFaceClient _client;
    private readonly IFaceEmbeddingRepository _embeddings;
    private readonly IRepository<Face> _faces;
    private readonly IFaceStorageService _storage;
    private readonly LocalInsightFaceOptions _opts;
    private readonly ILogger<LocalInsightFaceProvider> _log;

    public LocalInsightFaceProvider(
        ILocalInsightFaceClient client,
        IFaceEmbeddingRepository embeddings,
        IRepository<Face> faces,
        IFaceStorageService storage,
        Microsoft.Extensions.Options.IOptions<LocalInsightFaceOptions> opts,
        ILogger<LocalInsightFaceProvider> log)
    {
        _client = client;
        _embeddings = embeddings;
        _faces = faces;
        _storage = storage;
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
        var resp = await _client.DetectAsync(image, ct, includeEmbeddings: true);
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
            Embedding: f.Embedding,  // Pass through embedding from Python API!
            Emotion: f.Emotion,       // Pass through emotion if available
            Pose: f.Pose              // Pass through pose if available
        )).ToList();
    }

    public async Task<IReadOnlyList<IdentifyResultDto>> IdentifyAsync(IReadOnlyList<string> providerFaceIds, CancellationToken ct)
    {
        if (providerFaceIds == null || providerFaceIds.Count == 0)
            return Array.Empty<IdentifyResultDto>();

        _log.LogInformation("Starting face identification for {Count} face(s)", providerFaceIds.Count);

        // Parse face IDs from "local:123" or "123" format
        var faceIds = providerFaceIds
            .Select(id => int.TryParse(id.Replace("local:", ""), out var faceId) ? faceId : (int?)null)
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .ToList();

        if (faceIds.Count == 0)
        {
            _log.LogWarning("No valid face IDs found in provider face IDs");
            return Array.Empty<IdentifyResultDto>();
        }

        // Load Face records from database
        var faces = await _faces.GetAll()
            .Where(f => faceIds.Contains(f.Id) && !string.IsNullOrEmpty(f.S3Key_Image))
            .ToListAsync(ct);

        if (faces.Count == 0)
        {
            _log.LogWarning("No faces found in database for provided IDs");
            return Array.Empty<IdentifyResultDto>();
        }

        var results = new List<IdentifyResultDto>();
        using var sem = new SemaphoreSlim(_opts.MaxParallelism);

        var tasks = faces.Select(async face =>
        {
            await sem.WaitAsync(ct);
            try
            {
                var candidates = await IdentifySingleFaceAsync(face, ct);
                lock (results)
                {
                    results.Add(new IdentifyResultDto(
                        ProviderFaceId: $"local:{face.Id}",
                        Candidates: candidates
                    ));
                }
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Failed to identify face {FaceId}", face.Id);
                lock (results)
                {
                    results.Add(new IdentifyResultDto(
                        ProviderFaceId: $"local:{face.Id}",
                        Candidates: Array.Empty<IdentifyCandidateDto>()
                    ));
                }
            }
            finally { sem.Release(); }
        });

        await Task.WhenAll(tasks);

        _log.LogInformation("Completed identification for {Count} face(s), found {MatchCount} with candidates",
            faces.Count, results.Count(r => r.Candidates.Count > 0));

        return results;
    }

    private async Task<IReadOnlyList<IdentifyCandidateDto>> IdentifySingleFaceAsync(Face face, CancellationToken ct)
    {
        try
        {
            // Load face image from S3
            await using var stream = await _storage.OpenReadStreamAsync(face, ct);

            // Extract embedding using InsightFace API
            var embResp = await _client.EmbedAsync(stream, includeAttributes: false, ct);
            var embedding = Normalize(embResp.Embedding);

            // Find similar faces in database using pgvector
            var similarFaces = await _embeddings.FindSimilarFacesAsync(embedding, _opts.IdentifyMaxCandidates, ct);

            // Convert to candidates (distance -> confidence, filter by threshold)
            var candidates = similarFaces
                .Select(x => new IdentifyCandidateDto(
                    ProviderPersonId: $"local:{x.PersonId}",
                    Confidence: 1f - x.Distance  // Convert cosine distance to similarity
                ))
                .Where(c => c.Confidence >= _opts.FaceMatchThreshold)
                .OrderByDescending(c => c.Confidence)
                .ToList();

            if (candidates.Count > 0)
            {
                _log.LogInformation(
                    "Face {FaceId} matched {Count} candidate(s), best: PersonId={PersonId} with confidence={Confidence:F3}",
                    face.Id, candidates.Count, candidates[0].ProviderPersonId, candidates[0].Confidence);
            }
            else
            {
                _log.LogDebug("Face {FaceId} has no matches above threshold {Threshold}",
                    face.Id, _opts.FaceMatchThreshold);
            }

            return candidates;
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Error identifying single face {FaceId}", face.Id);
            return Array.Empty<IdentifyCandidateDto>();
        }
    }

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

