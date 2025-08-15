using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.CognitiveServices.Vision.Face.Models;
using PhotoBank.DbContext.Models;
using PhotoBank.Services.FaceRecognition;
using PhotoBank.Services.FaceRecognition.Abstractions;
using PhotoBank.Services.Models;

namespace PhotoBank.Services;

public sealed class FaceServiceAdapter : IFaceService
{
    private readonly UnifiedFaceService _svc;
    public FaceServiceAdapter(UnifiedFaceService svc) => _svc = svc;

    public Task SyncPersonsAsync() => _svc.SyncPersonsAsync();
    public Task SyncFacesToPersonAsync() => _svc.SyncFacesToPersonsAsync();

    // ниже — минимальные заглушки/проксирование; допиши при необходимости
    public Task AddFacesToLargeFaceListAsync() => Task.CompletedTask;
    public Task GroupIdentifyAsync() => Task.CompletedTask;
    public Task ListFindSimilarAsync() => Task.CompletedTask;

    public async Task<List<DetectedFace>> DetectFacesAsync(byte[] image)
        => (await _svc.DetectFacesAsync(image)).Select(d => new DetectedFace()).ToList(); // при желании сопоставь поля

    public Task<IList<IdentifyResult>> IdentifyAsync(IList<Guid?> faceIds) => Task.FromResult<IList<IdentifyResult>>(new List<IdentifyResult>());
    public Task<IdentifyResult> FaceIdentityAsync(Face face) => Task.FromResult<IdentifyResult>(null!);
}
