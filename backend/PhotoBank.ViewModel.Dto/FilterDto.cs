using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace PhotoBank.ViewModel.Dto
{
    public class FilterDto : PageRequest
    {
        public IEnumerable<int>? Storages { get; set; }
        public bool? IsBW { get; set; }
        public bool? IsAdultContent { get; set; }
        public bool? IsRacyContent { get; set; }
        public string? RelativePath { get; set; }
        public IEnumerable<int>? Paths { get; set; }
        public string? Caption { get; set; }
        public DateTime? TakenDateFrom { get; set; }
        public DateTime? TakenDateTo { get; set; }
        public ThisDayDto? ThisDay { get; set; }
        [JsonIgnore]
        public IEnumerable<int>? Persons { get; set; }
        public string[]? PersonNames { get; set; }
        [JsonIgnore]
        public IEnumerable<int>? Tags { get; set; }
        public string[]? TagNames { get; set; }

        public bool IsNotEmpty()
        {
            return (Storages != null && Storages.Any())
                   || (Persons != null && Persons.Any())
                   || (PersonNames != null && PersonNames.Any())
                   || (Tags != null && Tags.Any())
                   || (TagNames != null && TagNames.Any())
                   || (Paths != null && Paths.Any())
                   || !string.IsNullOrEmpty(RelativePath)
                   || IsBW.HasValue
                   || IsAdultContent.HasValue
                   || IsRacyContent.HasValue
                   || ThisDay != null
                   || TakenDateFrom.HasValue
                   || TakenDateTo.HasValue
                   || !string.IsNullOrEmpty(Caption);
        }
    }
}
