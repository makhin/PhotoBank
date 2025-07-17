using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PhotoBank.DbContext.Models
{
    public class PersonGroupFace : IEntityBase
    {
        public int Id { get; set; }
        public int PersonId { get; set; }
        public int FaceId { get; set; }
        public Person Person { get; set; }
        public Face Face { get; set; }
        public Guid ExternalGuid { get; set; }
    }
}