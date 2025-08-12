using System.Text.Json.Serialization;

namespace PhotoBank.ViewModel.Dto
{
    [JsonNumberHandling(JsonNumberHandling.Strict)]
    public class PageRequest
    {
        public int Page { get; init; } = 1; // Номер страницы, начиная с 1
        public int PageSize { get; init; } = 10; // Размер страницы
    }
}
