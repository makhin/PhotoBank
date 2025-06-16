namespace PhotoBank.ViewModel.Dto
{
    public class PathDto
    {
        [System.ComponentModel.DataAnnotations.Required]
        public int StorageId { get; set; }

        [System.ComponentModel.DataAnnotations.Required]
        public string Path { get; set; } = default!;
    }
}
