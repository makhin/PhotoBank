using System;
using System.Collections.Generic;
using System.Text;

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
