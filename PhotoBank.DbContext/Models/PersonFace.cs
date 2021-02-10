using System.ComponentModel.DataAnnotations.Schema;
using NetTopologySuite.Geometries;

namespace PhotoBank.DbContext.Models
{
    public class PersonFace : IEntityBase
    {
        public int Id { get; set; }
        public int? PersonId { get; set; }
        public Person Person { get; set; }
        public int PhotoId { get; set; }
        public Photo Photo { get; set; }
        public double? Age { get; set; }
        public bool? Gender { get; set; }
        public double? Smile { get; set; }
        public byte[] Image { get; set; }
        [Column(TypeName = "geometry")]
        public Geometry Rectangle { get; set; }
        public IdentityStatus IdentityStatus { get; set; }
        public double IdentifiedWithConfidence { get; set; }
        public string FaceAttributes { get; set; }
    }
}