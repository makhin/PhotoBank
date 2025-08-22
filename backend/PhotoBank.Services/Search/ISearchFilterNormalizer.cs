using System.Threading;
using System.Threading.Tasks;
using PhotoBank.ViewModel.Dto;

namespace PhotoBank.Services.Search;

public interface ISearchFilterNormalizer
{
    Task<FilterDto> NormalizeAsync(FilterDto filter, CancellationToken ct = default);
}
