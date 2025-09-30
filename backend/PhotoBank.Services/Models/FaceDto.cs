using PhotoBank.DbContext.Models;
using System;
using System.ComponentModel.DataAnnotations;

namespace PhotoBank.Services.Models
{
    public class FaceDto
    {
        [Required]
        public int Id { get; set; }
        public int? PersonId { get; set; }
        [Required]
        public IdentityStatusDto IdentityStatus { get; set; }
        public DateTime? PersonDateOfBirth { get; set; }
        public DateTime? PhotoTakenDate { get; set; }
    }
}
