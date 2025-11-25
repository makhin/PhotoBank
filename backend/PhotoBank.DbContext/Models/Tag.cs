using System.Collections.Generic;

namespace PhotoBank.DbContext.Models
{
    public class Tag : IEntityBase
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public string Hint { get; set; }

        public List<PhotoTag> PhotoTags { get; set; }

    }
}
