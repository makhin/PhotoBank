using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhotoBank.DbContext.Models
{
    public class FaceListFace
    {
        [Key, Column(Order = 0)]
        public int FaceListId { get; set; }
        [Key, Column(Order = 1)]
        public int FaceId { get; set; }
        public FaceList FaceList { get; set; }
        public Face Face { get; set; }
        public Guid ExternalGuid { get; set; }
    }
}
