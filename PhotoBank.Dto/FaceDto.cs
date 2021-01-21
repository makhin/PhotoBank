using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FaceRecognitionDotNet;
using PhotoBank.DbContext.Models;

namespace PhotoBank.Dto
{
    public class FaceDto 
    {
        public int Id { get; set; }
        public int? PersonId { get; set; }
        public byte[] Image { get; set; }
        public IdentityStatus IdentityStatus { get; set; }
        public DateTime? PersonDateOfBirth { get; set; }
        public DateTime? PhotoTakenDate { get; set; }
    }
}
