using System.Collections.Generic;

namespace PhotoBank.Dto.View
{
    public class QueryResult
    {
        public int Count { get; set; }
        public IEnumerable<PhotoItemDto> Photos { get; set; }
    }
}
