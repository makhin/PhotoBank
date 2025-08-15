using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace PhotoBank.Services.FaceRecognition.Abstractions;

public enum FaceProviderKind { Azure, Aws, Local }

public sealed record PersonSyncItem(int PersonId, string Name, string? ExternalId);
public sealed record FaceToLink(int FaceId, Func<Stream> OpenStream, string? ExternalId);

public sealed record DetectedFaceDto(string ProviderFaceId, float? Confidence, float? Age, string? Gender);
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

