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
        public byte[] PreviewImage { get; set; }
        public byte[] Thumbnail { get; set; }
        public int? Height { get; set; }
        public int? Width { get; set; }
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
        [MaxLength(255)]
        public string RelativePath { get; set; }
        public List<File> Files { get; set; }
        public double Scale { get; set; }
    }
}
