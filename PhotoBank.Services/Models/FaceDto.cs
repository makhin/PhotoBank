using System;
using PhotoBank.DbContext.Models;

namespace PhotoBank.Services.Models
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
