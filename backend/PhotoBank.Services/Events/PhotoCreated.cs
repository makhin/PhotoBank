using System.Collections.Generic;
using MediatR;

namespace PhotoBank.Services.Events;

public record PhotoCreated(
    int PhotoId,
    string StorageName,
    string RelativePath,
    byte[] Preview,
    byte[]? Thumbnail,
    IReadOnlyCollection<PhotoCreatedFace> Faces
) : INotification;

public record PhotoCreatedFace(int FaceId, byte[] Image);
