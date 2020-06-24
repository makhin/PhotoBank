using System;
using System.Collections.Generic;
using System.Text;

namespace PhotoBank.DbContext.Models
{
    public class Storage : IEntityBase
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public string Folder { get; set; }

        public List<Photo> Photos { get; set; }
    }
}
