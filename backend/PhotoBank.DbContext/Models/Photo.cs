using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NetTopologySuite.Geometries;

namespace PhotoBank.DbContext.Models
{
    public class Photo : IEntityBase
    {
        public Photo()
        {
            Scale = 1;
        }
        public int Id { get; set; }
        public int StorageId { get; set; }
        public Storage Storage { get; set; }
        [Required, MaxLength(255)]
        public string Name { get; set; }
        public DateTime? TakenDate { get; set; }
        public bool IsBW { get; set; }
        [MaxLength(6)]
        public string AccentColor { get; set; }
        [MaxLength(50)]
        public string DominantColorBackground { get; set; }
        [MaxLength(50)]
        public string DominantColorForeground { get; set; }
        [MaxLength(150)]
        public string DominantColors { get; set; }
        [Column(TypeName = "geometry")]
        public Point Location { get; set; }
        public byte[] Thumbnail { get; set; }
        [MaxLength(512)]
        public string S3Key_Preview { get; set; }
        [MaxLength(128)]
        public string S3ETag_Preview { get; set; }
        [MaxLength(64)]
        public string Sha256_Preview { get; set; }
        public long? BlobSize_Preview { get; set; }
        public DateTime? MigratedAt_Preview { get; set; }
        [MaxLength(512)]
        public string S3Key_Thumbnail { get; set; }
        [MaxLength(128)]
        public string S3ETag_Thumbnail { get; set; }
        [MaxLength(64)]
        public string Sha256_Thumbnail { get; set; }
        public long? BlobSize_Thumbnail { get; set; }
        public DateTime? MigratedAt_Thumbnail { get; set; }
        public uint? Height { get; set; }
        public uint? Width { get; set; }
        public int? Orientation { get; set; }
        public List<Caption> Captions { get; set; }
        public List<PhotoTag> PhotoTags { get; set; }
        public List<PhotoCategory> PhotoCategories { get; set; }
        public List<ObjectProperty> ObjectProperties { get; set; }
        public List<Face> Faces { get; set; }
        public bool IsAdultContent { get; set; }
        public double AdultScore { get; set; }
        public bool IsRacyContent { get; set; }
        public double RacyScore { get; set; }
        [MaxLength(256)]
        public string ImageHash { get; set; }
        [MaxLength(255)]
        public string RelativePath { get; set; }
        public List<File> Files { get; set; }
        public double Scale { get; set; }
        public FaceIdentifyStatus FaceIdentifyStatus { get; set; }
        public EnricherType EnrichedWithEnricherType { get; set; }
    }
}
