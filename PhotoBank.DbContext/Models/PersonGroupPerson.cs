using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PhotoBank.DbContext.Models
{
    public class PersonGroupPerson
    {
        [Key, Column(Order = 0)]
        public int PersonGroupId { get; set; }
        [Key, Column(Order = 1)]
        public int PersonId { get; set; }
        public PersonGroup PersonGroup { get; set; }
        public Person Person { get; set; }
        public Guid ExternalGuid { get; set; }
    }
}