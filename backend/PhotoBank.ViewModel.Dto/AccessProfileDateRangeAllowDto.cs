using System;
using System.ComponentModel.DataAnnotations;

namespace PhotoBank.ViewModel.Dto
{
    public class AccessProfileDateRangeAllowDto
    {
        [Required]
        public int ProfileId { get; set; }

        [Required]
        public DateOnly FromDate { get; set; }

        [Required]
        public DateOnly ToDate { get; set; }
    }
}
