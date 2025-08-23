using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PhotoBank.DbContext.Models
{
    public class PersonFace : IEntityBase
    {
        public int Id { get; set; }
        public int PersonId { get; set; }
        public int FaceId { get; set; }
        public Person Person { get; set; }
        public Face Face { get; set; }
        public string? Provider { get; set; }
        public string? ExternalId { get; set; } // вместо Guid для AWS/Local, Azure можно сохранять ToString()
        public Guid ExternalGuid { get; set; }
    }
}