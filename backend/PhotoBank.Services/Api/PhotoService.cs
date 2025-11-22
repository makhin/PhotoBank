using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using PhotoBank.Services.Photos;
using PhotoBank.Services.Photos.Admin;
using PhotoBank.Services.Photos.Faces;
using PhotoBank.Services.Photos.Queries;
using PhotoBank.ViewModel.Dto;

namespace PhotoBank.Services.Api;

public interface IPhotoService
{
    Task<PageResponse<PhotoItemDto>> GetAllPhotosAsync(FilterDto filter, CancellationToken ct = default);
    Task<PhotoDto> GetPhotoAsync(int id);
    Task<IEnumerable<PersonDto>> GetAllPersonsAsync();
    Task<IEnumerable<StorageDto>> GetAllStoragesAsync();
    Task<IEnumerable<TagDto>> GetAllTagsAsync();
    Task<IEnumerable<PathDto>> GetAllPathsAsync();
    Task<IEnumerable<PersonGroupDto>> GetAllPersonGroupsAsync();
    Task<PersonDto> CreatePersonAsync(string name);
    Task<PersonDto> UpdatePersonAsync(int personId, string name);
    Task DeletePersonAsync(int personId);
    Task<PersonGroupDto> CreatePersonGroupAsync(string name);
    Task<PersonGroupDto> UpdatePersonGroupAsync(int groupId, string name);
    Task DeletePersonGroupAsync(int groupId);
    Task AddPersonToGroupAsync(int groupId, int personId);
    Task RemovePersonFromGroupAsync(int groupId, int personId);
    Task<PageResponse<FaceDto>> GetFacesPageAsync(int page, int pageSize);
    Task<IEnumerable<FaceDto>> GetAllFacesAsync();
    Task UpdateFaceAsync(int faceId, int? personId);
    Task<IEnumerable<PhotoItemDto>> FindDuplicatesAsync(int? id, string? hash, int threshold);
    Task UploadPhotosAsync(IEnumerable<IFormFile> files, int storageId, string path);
}

public class PhotoService : IPhotoService
{
    private readonly IPhotoQueryService _photoQueryService;
    private readonly IPersonDirectoryService _personDirectoryService;
    private readonly IPersonGroupService _personGroupService;
    private readonly IFaceCatalogService _faceCatalogService;
    private readonly IPhotoDuplicateFinder _photoDuplicateFinder;
    private readonly IPhotoIngestionService _photoIngestionService;

    public PhotoService(
        IPhotoQueryService photoQueryService,
        IPersonDirectoryService personDirectoryService,
        IPersonGroupService personGroupService,
        IFaceCatalogService faceCatalogService,
        IPhotoDuplicateFinder photoDuplicateFinder,
        IPhotoIngestionService photoIngestionService)
    {
        _photoQueryService = photoQueryService;
        _personDirectoryService = personDirectoryService;
        _personGroupService = personGroupService;
        _faceCatalogService = faceCatalogService;
        _photoDuplicateFinder = photoDuplicateFinder;
        _photoIngestionService = photoIngestionService;
    }

    public Task<PageResponse<PhotoItemDto>> GetAllPhotosAsync(FilterDto filter, CancellationToken ct = default) =>
        _photoQueryService.GetAllPhotosAsync(filter, ct);

    public Task<PhotoDto> GetPhotoAsync(int id) => _photoQueryService.GetPhotoAsync(id);

    public Task<IEnumerable<PersonDto>> GetAllPersonsAsync() => _personDirectoryService.GetAllPersonsAsync();

    public Task<IEnumerable<StorageDto>> GetAllStoragesAsync() => _photoQueryService.GetAllStoragesAsync();

    public Task<IEnumerable<TagDto>> GetAllTagsAsync() => _photoQueryService.GetAllTagsAsync();

    public Task<IEnumerable<PathDto>> GetAllPathsAsync() => _photoQueryService.GetAllPathsAsync();

    public Task<IEnumerable<PersonGroupDto>> GetAllPersonGroupsAsync() => _personGroupService.GetAllPersonGroupsAsync();

    public Task<PersonDto> CreatePersonAsync(string name) => _personDirectoryService.CreatePersonAsync(name);

    public Task<PersonDto> UpdatePersonAsync(int personId, string name) =>
        _personDirectoryService.UpdatePersonAsync(personId, name);

    public Task DeletePersonAsync(int personId) => _personDirectoryService.DeletePersonAsync(personId);

    public Task<PersonGroupDto> CreatePersonGroupAsync(string name) =>
        _personGroupService.CreatePersonGroupAsync(name);

    public Task<PersonGroupDto> UpdatePersonGroupAsync(int groupId, string name) =>
        _personGroupService.UpdatePersonGroupAsync(groupId, name);

    public Task DeletePersonGroupAsync(int groupId) => _personGroupService.DeletePersonGroupAsync(groupId);

    public Task AddPersonToGroupAsync(int groupId, int personId) =>
        _personGroupService.AddPersonToGroupAsync(groupId, personId);

    public Task RemovePersonFromGroupAsync(int groupId, int personId) =>
        _personGroupService.RemovePersonFromGroupAsync(groupId, personId);

    public Task<PageResponse<FaceDto>> GetFacesPageAsync(int page, int pageSize) =>
        _faceCatalogService.GetFacesPageAsync(page, pageSize);

    public Task<IEnumerable<FaceDto>> GetAllFacesAsync() => _faceCatalogService.GetAllFacesAsync();

    public Task UpdateFaceAsync(int faceId, int? personId) => _faceCatalogService.UpdateFaceAsync(faceId, personId);

    public Task<IEnumerable<PhotoItemDto>> FindDuplicatesAsync(int? id, string? hash, int threshold) =>
        _photoDuplicateFinder.FindDuplicatesAsync(id, hash, threshold);

    public Task UploadPhotosAsync(IEnumerable<IFormFile> files, int storageId, string path) =>
        _photoIngestionService.UploadAsync(files, storageId, path);
}
