using System.Diagnostics.CodeAnalysis;

namespace PhotoBank.ViewModel.Dto
{
    public class TagDto
    {
        [System.ComponentModel.DataAnnotations.Required]

        public int Id { get; set; }

        [System.ComponentModel.DataAnnotations.Required]
        public string Name { get; set; } = default!;
    }
}
