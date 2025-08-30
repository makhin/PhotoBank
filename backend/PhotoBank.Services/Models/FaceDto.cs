using System;
using System.ComponentModel.DataAnnotations;
using PhotoBank.DbContext.Models;

namespace PhotoBank.Services.Models
{
    public class FaceDto
    {
        [Required]
        public int Id { get; set; }
        public int? PersonId { get; set; }
        [Required]
        public string S3Key_Image { get; set; }
        [Required]
        public IdentityStatus IdentityStatus { get; set; }
        public DateTime? PersonDateOfBirth { get; set; }
        public DateTime? PhotoTakenDate { get; set; }
    }
}
