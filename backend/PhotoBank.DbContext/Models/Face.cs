using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NetTopologySuite.Geometries;
using Pgvector;

namespace PhotoBank.DbContext.Models
{
    public class Face : IEntityBase
    {
        public int Id { get; set; }
        [Column(TypeName = "geometry")]
        public Geometry Rectangle { get; set; }
        public double? Age { get; set; }
        public bool? Gender { get; set; }
        public double? Smile { get; set; }
        [MaxLength(512)]
        public string S3Key_Image { get; set; }
        [MaxLength(128)]
        public string S3ETag_Image { get; set; }
        [MaxLength(64)]
        public string Sha256_Image { get; set; }
        public long? BlobSize_Image { get; set; }
        public DateTime? MigratedAt_Image { get; set; }
        [MaxLength(64)]
        public string? Provider { get; set; }
        [MaxLength(256)]
        public string? ExternalId { get; set; }
        public Guid ExternalGuid { get; set; }
        public int? PersonId { get; set; }
        public Person Person { get; set; }
        public int PhotoId { get; set; }
        public Photo Photo { get; set; }
        public IdentityStatus IdentityStatus { get; set; }
        public double IdentifiedWithConfidence { get; set; }
        public string FaceAttributes { get; set; }
        [Column(TypeName = "vector(512)")]
        public Vector? Embedding { get; set; }
    }
}
