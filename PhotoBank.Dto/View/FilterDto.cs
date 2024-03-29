﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhotoBank.Dto.View
{
    public class FilterDto
    {
        public IEnumerable<int> Storages { get; set; }
        public bool? IsBW { get; set; }
        public bool? IsAdultContent { get; set; }
        public bool? IsRacyContent { get; set; }
        public string RelativePath { get; set; }
        public IEnumerable<int> Paths { get; set; }
        public string Caption { get; set; }
        public DateTime? TakenDateFrom { get; set; }
        public DateTime? TakenDateTo { get; set; }
        public bool? ThisDay { get; set; }
        public IEnumerable<int> Persons { get; set; }
        public IEnumerable<int> Tags { get; set; }
    }
}
