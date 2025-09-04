using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace PhotoBank.ViewModel.Dto
{
    public class PhotoItemDto : IHasId<int>
    {
        [System.ComponentModel.DataAnnotations.Required]
        public int Id { get; set; }

        [JsonIgnore]
        public string S3Key_Thumbnail { get; set; }
        public string? ThumbnailUrl { get; set; }

        [System.ComponentModel.DataAnnotations.Required]
        public required string Name { get; set; }
        public DateTime? TakenDate { get; set; }
        public bool IsBW { get; set; }
        public bool IsAdultContent { get; set; }
        public double AdultScore { get; set; }
        public bool IsRacyContent { get; set; }
        public double RacyScore { get; set; }
        [System.ComponentModel.DataAnnotations.Required]
        public required string StorageName { get; set; }

        [System.ComponentModel.DataAnnotations.Required]
        public required string RelativePath { get; set; }
        public IEnumerable<TagItemDto>? Tags { get; set; }
        public IEnumerable<PersonItemDto>? Persons { get; set; }
        public List<string>? Captions { get; set; }
    }
}
