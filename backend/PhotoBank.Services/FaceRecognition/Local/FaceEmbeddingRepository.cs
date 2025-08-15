using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using EfDbContext = Microsoft.EntityFrameworkCore.DbContext;

namespace PhotoBank.Services.FaceRecognition.Local;

public sealed class FaceEmbeddingRepository : IFaceEmbeddingRepository
{
    private readonly EfDbContext _db;
    public FaceEmbeddingRepository(EfDbContext db) => _db = db;

    public async Task UpsertAsync(int personId, int faceId, float[] vector, string model, CancellationToken ct)
    {
        var bytes = MemoryMarshal.AsBytes(vector.AsSpan()).ToArray();
        var set = _db.Set<FaceEmbedding>();
        var entity = await set.FirstOrDefaultAsync(x => x.FaceId == faceId, ct);
        if (entity is null)
        {
            entity = new FaceEmbedding { FaceId = faceId, PersonId = personId, Model = model, Vector = bytes };
            set.Add(entity);
        }
        else
        {
            entity.PersonId = personId;
            entity.Model = model;
            entity.Vector = bytes;
        }
        await _db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<(int PersonId, int FaceId, float[] Vector)>> GetAllAsync(CancellationToken ct)
    {
        var rows = await _db.Set<FaceEmbedding>()
            .AsNoTracking()
            .Select(x => new { x.PersonId, x.FaceId, x.Vector })
            .ToListAsync(ct);
        return rows.Select(x => (x.PersonId, x.FaceId, MemoryMarshal.Cast<byte, float>(x.Vector.AsSpan()).ToArray())).ToList();
    }
}
