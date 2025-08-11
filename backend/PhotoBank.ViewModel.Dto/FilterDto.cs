using System;
using System.Collections.Generic;
using System.Linq;

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
        public bool? ThisDay { get; set; }
        public IEnumerable<int>? Persons { get; set; }
        public IEnumerable<int>? Tags { get; set; }

        public string? OrderBy { get; set; }

        public bool IsNotEmpty()
        {
            return (Storages != null && Storages.Any()) || (Persons!= null && Persons.Any()) || (Tags!= null && Tags.Any()) || IsBW.HasValue || IsAdultContent.HasValue || IsRacyContent.HasValue || ThisDay.HasValue || TakenDateFrom.HasValue || TakenDateTo.HasValue || !string.IsNullOrEmpty(Caption);
        }
    }
}
