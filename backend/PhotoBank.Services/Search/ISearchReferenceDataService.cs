using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PhotoBank.ViewModel.Dto;

namespace PhotoBank.Services.Search;

public interface ISearchReferenceDataService
{
    Task<IReadOnlyList<PersonDto>> GetPersonsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TagDto>> GetTagsAsync(CancellationToken cancellationToken = default);
    void InvalidatePersonsCache();
}
