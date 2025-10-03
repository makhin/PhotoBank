using PhotoBank.Services.Models;
using System.Text.Json.Serialization;

namespace PhotoBank.ViewModel.Dto
{
    public class FaceDto : IHasId<int>
    {
        public int Id { get; set; }
        public int? PersonId { get; set; }
        public double? Age { get; set; }
        public bool? Gender { get; set; }
        public double? Smile { get; set; }
        public string? FaceAttributes { get; set; }
        public FaceBoxDto? FaceBox { get; set; }
        public string? FriendlyFaceAttributes { get; set; }
        public string? Provider { get; set; }
        public int PhotoId { get; set; }
        public double IdentifiedWithConfidence { get; set; }
        public IdentityStatusDto IdentityStatus { get; set; }
        [JsonIgnore]
        public string S3Key_Image { get; set; }
        public string? ImageUrl { get; set; }

    }
}
