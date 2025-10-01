using System.ComponentModel.DataAnnotations;

namespace PhotoBank.ViewModel.Dto
{
    public class AccessProfileStorageAllowDto
    {
        [Required]
        public int ProfileId { get; set; }

        [Required]
        public int StorageId { get; set; }
    }
}
