using System;
using System.Collections.Generic;
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
        public double AdultScore { get; set; }
        public bool IsRacyContent { get; set; }
        public double RacyScore { get; set; }
        public string StorageName { get; set; }
        public string RelativePath { get; set; }
        public IEnumerable<TagItemDto> Tags { get; set; }
        public IEnumerable<PersonItemDto> Persons { get; set; }
    }
}
