using System;
using System.ComponentModel.DataAnnotations.Schema;
using NetTopologySuite.Geometries;

namespace PhotoBank.DbContext.Models
{
    public class Face : IEntityBase
    {
        public int Id { get; set; }
        [Column(TypeName = "geometry")]
        public Geometry Rectangle { get; set; }
        public int Age { get; set; }
        public int? Gender { get; set; }
        public byte[] Image { get; set; }
        public Person Person { get; set; }
        public bool? IsSample { get; set; }
        public Guid? ExternalGuid { get; set; } 
        public byte[] Encoding { get; set; }
        public double CheckedWithTolerance { get; set; }
        public bool IsExcluded { get; set; }
    }
}
