using System;
using System.Collections.Generic;
using System.Text;

namespace PhotoBank.DbContext.Models
{
    public class Category : IEntityBase
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public List<PhotoCategory> PhotoCategories { get; set; }
    }
}
