using System;
using System.Collections.Generic;

namespace PhotoBank.ViewModel.Dto
{
    public class PersonGroupDto : IHasId<int>
    {
        [System.ComponentModel.DataAnnotations.Required]
        public int Id { get; set; }

        [System.ComponentModel.DataAnnotations.Required]
        public required string Name { get; set; } = default!;

        public IReadOnlyCollection<PersonDto> Persons { get; set; } = Array.Empty<PersonDto>();
    }
}

