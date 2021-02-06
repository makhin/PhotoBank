using System;
using System.ComponentModel.DataAnnotations.Schema;
using NetTopologySuite.Geometries;

namespace PhotoBank.DbContext.Models
{
    public class Face : IEntityBase
    {
        public int Id { get; set; }
        [Column(TypeName = "geometry")] public Geometry Rectangle { get; set; }
        public int Age { get; set; }
        public int? Gender { get; set; }
        public byte[] Image { get; set; }
        public PersonGroupFace PersonGroupFace { get; set; }
        public int? PersonId { get; set; }
        public Person Person { get; set; }
        public int PhotoId { get; set; }
        public Photo Photo { get; set; }
        public Guid? ExternalGuid { get; set; }
        public ListStatus ListStatus { get; set; }
        public IdentityStatus IdentityStatus { get; set; }
        public double IdentifiedWithConfidence { get; set; }
        public byte[] Encoding { get; set; }
    }
}
