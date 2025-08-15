using System.Text.Json.Serialization;

namespace PhotoBank.ViewModel.Dto
{
    [JsonNumberHandling(JsonNumberHandling.Strict)]
    public class PageRequest
    {
        public const int MaxPageSize = 200;

        public int Page { get; init; } = 1; // Номер страницы, начиная с 1

        private int _pageSize = 10;
        public int PageSize
        {
            get => _pageSize;
            init => _pageSize = value > MaxPageSize ? MaxPageSize : value; // Размер страницы с ограничением
        }
    }
}
