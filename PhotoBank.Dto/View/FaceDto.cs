namespace PhotoBank.Dto.View
{
    public class FaceDto 
    {
        public int Id { get; set; }
        public int? PersonId { get; set; }
        public FaceBoxDto FaceBox { get; set; }
    }
}
