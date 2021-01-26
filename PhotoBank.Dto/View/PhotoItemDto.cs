using System;
using PhotoBank.DbContext.Models;

namespace PhotoBank.Dto.View
{
    public class PhotoItemDto : IEntityBase
    {
        public int Id { get; set; }
        public byte[] Thumbnail { get; set; }
        public string Name { get; set; }
        public DateTime? TakenDate { get; set; }
    }
}
