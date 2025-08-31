namespace PhotoBank.ViewModel.Dto
{
    public class PersonFaceDto : IHasId<int>
    {
        public int Id { get; set; }

        [System.ComponentModel.DataAnnotations.Required]
        public int PersonId { get; set; }

        [System.ComponentModel.DataAnnotations.Required]
        public int FaceId { get; set; }

        public string? Provider { get; set; }
        public string? ExternalId { get; set; }
        public System.Guid ExternalGuid { get; set; }
    }
}
