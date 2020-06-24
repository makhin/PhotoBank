using System;
using System.Collections.Generic;
using System.Text;

namespace PhotoBank.DbContext.Models
{
    public class PhotoCategory
    {
        public int PhotoId { get; set; }
        public Photo Photo { get; set; }
        public int CategoryId { get; set; }
        public Category Category { get; set; }
        public double Score { get; set; }
    }
}
