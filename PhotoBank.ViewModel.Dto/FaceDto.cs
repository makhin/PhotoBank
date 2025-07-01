
namespace PhotoBank.ViewModel.Dto
{
    public class FaceDto 
    {
        public int Id { get; set; }
        public int? PersonId { get; set; }
        public double? Age { get; set; }
        public bool? Gender { get; set; }
        public string? FaceAttributes { get; set; }
        [System.ComponentModel.DataAnnotations.Required]
        public required FaceBoxDto FaceBox { get; set; }

        [System.ComponentModel.DataAnnotations.Required]
        public required string FriendlyFaceAttributes { get;set; }
    }
}
