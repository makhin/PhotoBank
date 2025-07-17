using System.Collections.Generic;

namespace PhotoBank.ViewModel.Dto
{
    public class QueryResult
    {
        public int Count { get; set; }
        public IEnumerable<PhotoItemDto>? Photos { get; set; }
    }
}
