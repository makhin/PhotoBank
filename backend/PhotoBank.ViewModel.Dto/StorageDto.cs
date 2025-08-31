using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhotoBank.ViewModel.Dto
{
    public class StorageDto : IHasId<int>
    {
        [System.ComponentModel.DataAnnotations.Required]

        public int Id { get; set; }

        [System.ComponentModel.DataAnnotations.Required] 
        public string Name { get; set; } = default!;
    }
}
