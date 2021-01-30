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
        public bool IsBW { get; set; }
        public bool IsAdultContent { get; set; }
        public bool IsRacyContent { get; set; }
        public string RelativePath { get; set; }
    }
}
