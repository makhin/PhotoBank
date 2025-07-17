namespace PhotoBank.ViewModel.Dto
{
    public class FaceBoxDto
    {
        [System.ComponentModel.DataAnnotations.Required]
        public required int Top { set; get; }
        [System.ComponentModel.DataAnnotations.Required]
        public required int Left { set; get; }
        [System.ComponentModel.DataAnnotations.Required]
        public required int Width { set; get; }
        [System.ComponentModel.DataAnnotations.Required]
        public required int Height { set; get; }
    }
}