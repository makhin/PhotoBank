using System;
using System.Collections.Generic;
using System.Text;

namespace PhotoBank.DbContext.Models
{
    public class Caption : IEntityBase
    {
        public int Id { get; set; }

        public string Text { get; set; }

        public double Confidence { get; set; }
    }
}
