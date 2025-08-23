using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PhotoBank.DbContext.Models
{
    public class Person : IEntityBase
    {
        public int Id { get; set; }
        [Required]
        public string Name { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? Provider { get; set; }  // "Azure" | "Aws" | "Local"
        public string? ExternalId { get; set; } // строковый внешний ID провайдера
        public Guid ExternalGuid { get; set; }
        public IEnumerable<PersonFace> PersonFaces { get; set; }
        public IEnumerable<Face> Faces { get; set; }
        public ICollection<PersonGroup> PersonGroups { get; set; }
    }
}
