using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PhotoBank.DbContext.Models
{
    public class PersonGroup : IEntityBase
    {
        public int Id { get; set; }
        [Required]
        public string Name { get; set; }
        public ICollection<Person> Persons { get; set; }
    }
}
