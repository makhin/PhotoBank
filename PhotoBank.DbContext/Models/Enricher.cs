using System.ComponentModel.DataAnnotations;

namespace PhotoBank.DbContext.Models
{
    public class Enricher: IEntityBase
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public bool IsActive { get; set; }
    }
}
