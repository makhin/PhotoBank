namespace PhotoBank.ViewModel.Dto
{
    public class PersonGroupDto
    {
        [System.ComponentModel.DataAnnotations.Required]
        public int Id { get; set; }

        [System.ComponentModel.DataAnnotations.Required]
        public required string Name { get; set; } = default!;
    }
}

