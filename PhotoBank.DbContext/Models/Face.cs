using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using NetTopologySuite.Geometries;

namespace PhotoBank.DbContext.Models
{
    public class Face : IEntityBase
    {
        public int Id { get; set; }
        public string Name { get; set; }
        [Column(TypeName = "geometry")]
        public Geometry Rectangle { get; set; }
        public int Age { get; set; }
        public int? Gender { get; set; }
    }
}
