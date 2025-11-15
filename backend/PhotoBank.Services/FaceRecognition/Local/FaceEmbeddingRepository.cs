using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Pgvector;
using PhotoBank.DbContext.Models;

namespace PhotoBank.Services.FaceRecognition.Local;

public sealed class FaceEmbeddingRepository : IFaceEmbeddingRepository
{
    private readonly DbContext.DbContext.PhotoBankDbContext _db;

    public FaceEmbeddingRepository(DbContext.DbContext.PhotoBankDbContext db) => _db = db;

    public async Task UpsertAsync(int personId, int faceId, float[] vector, string model, CancellationToken ct)
    {
        var face = await _db.Faces.FirstOrDefaultAsync(x => x.Id == faceId, ct);
        if (face is null)
        {
            throw new InvalidOperationException($"Face with ID {faceId} not found");
        }

        face.Embedding = new Vector(vector);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<(int PersonId, int FaceId, float[] Vector)>> GetAllAsync(CancellationToken ct)
    {
        var faces = await _db.Faces
            .AsNoTracking()
            .Where(x => x.Embedding != null && x.PersonId != null && x.IdentityStatus == IdentityStatus.Identified)
            .Select(x => new { x.PersonId, x.Id, x.Embedding })
            .ToListAsync(ct);

        return faces
            .Where(x => x.PersonId.HasValue && x.Embedding != null)
            .Select(x => (x.PersonId!.Value, x.Id, x.Embedding!.ToArray()))
            .ToList();
    }
}
