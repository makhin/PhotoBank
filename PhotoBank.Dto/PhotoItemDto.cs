using System;
using System.Collections.Generic;
using System.Text;
using PhotoBank.DbContext.Models;

namespace PhotoBank.Dto
{
    public class PhotoItemDto : IEntityBase
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public DateTime? TakenDate { get; set; }
    }
}
