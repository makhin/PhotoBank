using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace PhotoBank.Services.FaceRecognition.Abstractions;

public enum FaceProviderKind { Azure, Aws, Local }

public sealed record PersonSyncItem(int PersonId, string Name, string? ExternalId);
public sealed record FaceToLink(int FaceId, Func<Stream> OpenStream, string? ExternalId);

/// <summary>
/// Unified bounding box representation for face detection across all providers.
/// All values are normalized (0.0 to 1.0) relative to image dimensions.
/// </summary>
public sealed record FaceBoundingBox(float Left, float Top, float Width, float Height);

/// <summary>
/// Emotion recognition scores from face detection
/// 8 emotion classes: anger, contempt, disgust, fear, happiness, neutral, sadness, surprise
/// Supported by Local (InsightFace + HSEmotion) provider
/// </summary>
public sealed record EmotionScoresDto(
    float? Anger,
    float? Contempt,
    float? Disgust,
    float? Fear,
    float? Happiness,
    float? Neutral,
    float? Sadness,
    float? Surprise
);

public sealed record DetectedFaceDto(
    string ProviderFaceId,
    float? Confidence,
    float? Age,
    string? Gender,
    FaceBoundingBox? BoundingBox,
    string? Emotion,
    EmotionScoresDto? EmotionScores
);

public sealed record IdentifyCandidateDto(string ProviderPersonId, float Confidence);
public sealed record IdentifyResultDto(string ProviderFaceId, IReadOnlyList<IdentifyCandidateDto> Candidates);
public sealed record UserMatchDto(string ProviderPersonId, float Confidence);

public interface IFaceProvider
{
    FaceProviderKind Kind { get; }

    Task EnsureReadyAsync(CancellationToken ct);

    Task<IReadOnlyDictionary<int, string>> UpsertPersonsAsync(
        IReadOnlyCollection<PersonSyncItem> persons,
        CancellationToken ct);

    Task<IReadOnlyDictionary<int, string>> LinkFacesToPersonAsync(
        int personId,
        IReadOnlyCollection<FaceToLink> faces,
        CancellationToken ct);

    Task<IReadOnlyList<DetectedFaceDto>> DetectAsync(Stream image, CancellationToken ct);

    // Azure реализует Identify по faceIds; AWS/Local могут возвращать пусто (используем SearchUsersByImageAsync)
    Task<IReadOnlyList<IdentifyResultDto>> IdentifyAsync(IReadOnlyList<string> providerFaceIds, CancellationToken ct);

    Task<IReadOnlyList<UserMatchDto>> SearchUsersByImageAsync(Stream image, CancellationToken ct);
}

