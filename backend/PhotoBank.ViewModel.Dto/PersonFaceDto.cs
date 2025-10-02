namespace PhotoBank.ViewModel.Dto
{
    public class PersonFaceDto : IHasId<int>
    {
        private int _faceId;

        public int Id
        {
            get => _faceId;
            set => _faceId = value;
        }

        [System.ComponentModel.DataAnnotations.Required]
        public int PersonId { get; set; }

        [System.ComponentModel.DataAnnotations.Required]
        public int FaceId
        {
            get => _faceId;
            set => _faceId = value;
        }

        public string? Provider { get; set; }
        public string? ExternalId { get; set; }
        public System.Guid ExternalGuid { get; set; }
    }
}
