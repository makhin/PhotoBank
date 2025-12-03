using System.ComponentModel.DataAnnotations;

namespace PhotoBank.DbContext.Models
{
    public class File : IEntityBase
    {
        public int Id { get; set; }
        [Required, MaxLength(255)]
        public string Name { get; set; }
        public int PhotoId { get; set; }
        public Photo Photo { get; set; }

        // Multi-storage duplicate support
        public int StorageId { get; set; }
        public Storage Storage { get; set; }
        [Required, MaxLength(255)]
        public string RelativePath { get; set; }

        public bool IsDeleted { get; set; }
    }
}
