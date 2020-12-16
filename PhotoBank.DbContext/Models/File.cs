using System.ComponentModel.DataAnnotations;

namespace PhotoBank.DbContext.Models
{
    public class File : IEntityBase
    {
        public int Id { get; set; }
        [Required, MaxLength(255)]
        public string Name { get; set; } 
    }
}
