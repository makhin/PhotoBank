using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PhotoBank.Services.FaceRecognition.Abstractions;

namespace PhotoBank.Services.FaceRecognition;

/// <summary>
/// Unified interface for face recognition service that works with any face provider.
/// </summary>
public interface IUnifiedFaceService
{
    /// <summary>
    /// Gets the kind of face recognition provider being used.
    /// </summary>
    FaceProviderKind ProviderKind { get; }

    /// <summary>
    /// Ensures the face recognition provider is ready (e.g., creates collections if needed).
    /// </summary>
    Task EnsureReadyAsync(CancellationToken ct = default);

    /// <summary>
    /// Synchronizes persons from the database to the face recognition provider.
    /// </summary>
    Task SyncPersonsAsync(CancellationToken ct = default);

    /// <summary>
    /// Synchronizes face-to-person associations from the database to the provider.
    /// </summary>
    Task SyncFacesToPersonsAsync(CancellationToken ct = default);

    /// <summary>
    /// Detects faces in the provided image.
    /// </summary>
    /// <param name="image">Image bytes to analyze.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of detected faces with their attributes.</returns>
    Task<IReadOnlyList<DetectedFaceDto>> DetectFacesAsync(byte[] image, CancellationToken ct = default);
}
