using System;
using System.Collections.Generic;

namespace PhotoBank.ViewModel.Dto
{
    public class PageResponse<T>
    {
        public int TotalCount { get; set; }
        public IReadOnlyCollection<T> Items { get; set; } = Array.Empty<T>();
    }
}
