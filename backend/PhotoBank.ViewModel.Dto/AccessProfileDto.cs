using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PhotoBank.ViewModel.Dto
{
    public class AccessProfileDto : IHasId<int>
    {
        [Required]
        public int Id { get; set; }

        [Required]
        [MaxLength(128)]
        public string Name { get; set; } = default!;

        [MaxLength(512)]
        public string? Description { get; set; }

        public bool Flags_CanSeeNsfw { get; set; }

        public ICollection<AccessProfileStorageAllowDto> Storages { get; set; } = new List<AccessProfileStorageAllowDto>();

        public ICollection<AccessProfilePersonGroupAllowDto> PersonGroups { get; set; } = new List<AccessProfilePersonGroupAllowDto>();

        public ICollection<AccessProfileDateRangeAllowDto> DateRanges { get; set; } = new List<AccessProfileDateRangeAllowDto>();
    }
}
