using PhotoBank.ViewModel.Dto;

namespace PhotoBank.MAUI.Blazor.Services
{
    internal interface IRestService
    {
        Task<QueryResult> GetPhotos(FilterDto request);
        Task<PhotoDto> GetPhoto(int photoId);
        Task<IEnumerable<StorageDto>> GetStorages();
        Task<IEnumerable<PathDto>> GetPaths();
        Task<IEnumerable<PersonDto>> GetPersons();
        Task<IEnumerable<TagDto>> GetTags();
    }
}
