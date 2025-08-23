namespace PhotoBank.ViewModel.Dto
{
    public class PersonGroupFaceDto
    {
        public int Id { get; set; }

        [System.ComponentModel.DataAnnotations.Required]
        public int PersonId { get; set; }

        [System.ComponentModel.DataAnnotations.Required]
        public int FaceId { get; set; }

        public byte[]? FaceImage { get; set; }

        public string? Provider { get; set; }
        public string? ExternalId { get; set; }
        public System.Guid ExternalGuid { get; set; }
    }
}
