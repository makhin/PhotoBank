namespace PhotoBank.ViewModel.Dto
{
    public class PageRequest
    {
        public int Page { get; set; } = 1; // Номер страницы, начиная с 1
        public int PageSize { get; set; } = 10; // Размер страницы
    }
}
