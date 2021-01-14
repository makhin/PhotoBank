using System.ComponentModel.DataAnnotations;

namespace PhotoBank.DbContext.Models
{
    public class FaceToFace
    {
        public int Face1Id { get; set; }
        public int Face2Id { get; set; }
        public double Distance { get; set; }
    }
}
