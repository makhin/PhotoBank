using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FaceRecognitionDotNet;

namespace PhotoBank.Dto
{
    public class FaceDto 
    {
        public int Id { get; set; }
        public int? PersonId { get; set; }
        public int Age { get; set; }
        public int? Gender { get; set; }
        public FaceEncoding FaceEncoding { get; set; }
    }
}
