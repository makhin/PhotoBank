using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using NetTopologySuite.Geometries;

namespace PhotoBank.DbContext.Models
{
    public class ObjectProperty : IEntityBase
    {
        public int Id { get; set; }

        [Column(TypeName = "geometry")]
        public Geometry Rectangle { get; set; }
        public double Confidence { get; set; }
        public string Name { get; set; }
    }
}
