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

namespace PhotoBank.Services.FaceRecognition;

public sealed class UnifiedFaceService
{
    private readonly IFaceProvider _provider;
    private readonly IRepository<Person> _persons;
    private readonly IRepository<Face> _faces;
    private readonly IRepository<PersonFace> _links;
    private readonly ILogger<UnifiedFaceService> _log;
    private readonly IFaceStorageService _storage;

    public UnifiedFaceService(
        IFaceProvider provider,
        IRepository<Person> persons,
        IRepository<Face> faces,
        IRepository<PersonFace> links,
        IFaceStorageService storage,
        ILogger<UnifiedFaceService> log)
    {
        _provider = provider;
        _persons = persons;
        _faces = faces;
        _links = links;
        _storage = storage;
        _log = log;
    }

    public Task EnsureReadyAsync(CancellationToken ct = default) => _provider.EnsureReadyAsync(ct);

    public async Task SyncPersonsAsync(CancellationToken ct = default)
    {
        var dbPersons = await _persons.GetAll()
            .AsNoTracking()
            .Select(p => new { p.Id, p.Name, p.ExternalId, p.Provider })
            .ToListAsync(ct);

        var items = dbPersons
            .Where(p => p.Provider == null || p.Provider == _provider.Kind.ToString())
            .Select(p => new PersonSyncItem(p.Id, p.Name, p.ExternalId))
            .ToList();

        var map = await _provider.UpsertPersonsAsync(items, ct);
        foreach (var (personId, external) in map)
        {
            await _persons.UpdateAsync(new Person
            {
                Id = personId,
                ExternalId = external,
                Provider = _provider.Kind.ToString()
            }, p => p.ExternalId, p => p.Provider);
        }
    }

    public async Task SyncFacesToPersonsAsync(CancellationToken ct = default)
    {
        var links = await _links.GetAll()
            .AsNoTracking()
            .Select(l => new { l.Id, l.PersonId, l.FaceId, l.ExternalId, l.Provider })
            .ToListAsync(ct);

        foreach (var group in links.GroupBy(l => l.PersonId))
        {
            var missing = group.Where(g => string.IsNullOrEmpty(g.ExternalId)).ToList();
            if (missing.Count == 0) continue;

            var faceIds = missing.Select(m => m.FaceId).ToArray();

            var faces = await _faces.GetAll()
                .Where(f => faceIds.Contains(f.Id))
                .Select(f => new Face { Id = f.Id, S3Key_Image = f.S3Key_Image })
                .ToListAsync(ct);

            var toLink = new List<FaceToLink>();
            foreach (var face in faces)
            {
                await using var stream = await _storage.OpenReadStreamAsync(face, ct);
                var ms = new MemoryStream();
                await stream.CopyToAsync(ms, ct);
                var bytes = ms.ToArray();
                toLink.Add(new FaceToLink(
                    face.Id,
                    OpenStream: () => new MemoryStream(bytes, writable: false),
                    ExternalId: null));
            }

            var map = await _provider.LinkFacesToPersonAsync(group.Key, toLink, ct);
            foreach (var (faceId, external) in map)
            {
                var linkId = missing.Single(m => m.FaceId == faceId).Id;

                await _links.UpdateAsync(new PersonFace
                {
                    Id = linkId,
                    PersonId = group.Key,
                    FaceId = faceId,
                    ExternalId = external,
                    Provider = _provider.Kind.ToString()
                }, x => x.ExternalId, x => x.Provider);
            }
        }
    }

    public async Task<IReadOnlyList<DetectedFaceDto>> DetectFacesAsync(byte[] image, CancellationToken ct = default)
    {
        using var ms = new MemoryStream(image, writable: false);
        return await _provider.DetectAsync(ms, ct);
    }
}

