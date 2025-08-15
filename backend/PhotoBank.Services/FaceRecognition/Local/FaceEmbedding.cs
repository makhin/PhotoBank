using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PhotoBank.Services.FaceRecognition.Local;

public class FaceEmbedding
{
    public int FaceId { get; set; }
    public int PersonId { get; set; }
    public string Model { get; set; } = "buffalo_l";
    public byte[] Vector { get; set; } = Array.Empty<byte>();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class FaceEmbeddingConfiguration : IEntityTypeConfiguration<FaceEmbedding>
{
    public void Configure(EntityTypeBuilder<FaceEmbedding> b)
    {
        b.ToTable("FaceEmbeddings");
        b.HasKey(x => x.FaceId);
        b.Property(x => x.Vector).HasColumnType("varbinary(max)");
        b.HasIndex(x => x.PersonId);
    }
}

public interface IFaceEmbeddingRepository
{
    Task UpsertAsync(int personId, int faceId, float[] vector, string model, CancellationToken ct);
    Task<IReadOnlyList<(int PersonId, int FaceId, float[] Vector)>> GetAllAsync(CancellationToken ct);
}
