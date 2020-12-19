using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhotoBank.DbContext.Models
{
    public class PersonGroup : IEntityBase
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public Guid ExternalGuid { get; set; }

        public ICollection<PersonGroupPerson> PersonGroupPersons { get; set; }
    }
}
