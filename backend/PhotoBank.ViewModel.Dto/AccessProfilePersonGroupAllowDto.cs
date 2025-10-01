using System.ComponentModel.DataAnnotations;

namespace PhotoBank.ViewModel.Dto
{
    public class AccessProfilePersonGroupAllowDto
    {
        [Required]
        public int ProfileId { get; set; }

        [Required]
        public int PersonGroupId { get; set; }
    }
}
