using System;
using System.Collections.Generic;
using System.Text;

namespace PhotoBank.DbContext.Models
{
    public class PhotoTag
    {
        public int PhotoId { get; set; }
        public Photo Photo { get; set; }
        public int TagId { get; set; }
        public Tag Tag { get; set; }
        public double Confidence { get; set; }
    }
}
