using System;
using PhotoBank.DbContext.Models;

namespace PhotoBank.ViewModel.Dto
{
    public class FaceDto : IHasId<int>
    {
        public int Id { get; set; }
        public int? PersonId { get; set; }
        public double? Age { get; set; }
        public bool? Gender { get; set; }
        public string? FaceAttributes { get; set; }
        public FaceBoxDto? FaceBox { get; set; }
        public string? FriendlyFaceAttributes { get; set; }
        public string? Provider { get; set; }
        public string? ExternalId { get; set; }
        public Guid ExternalGuid { get; set; }
        public int PhotoId { get; set; }
        public double IdentifiedWithConfidence { get; set; }
        public IdentityStatus IdentityStatus { get; set; }
    }
}
