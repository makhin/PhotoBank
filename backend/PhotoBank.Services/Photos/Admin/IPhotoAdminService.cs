using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using PhotoBank.Services.Photos;

namespace PhotoBank.Services.Photos.Admin;

public interface IPhotoAdminService
{
    Task UploadPhotosAsync(IEnumerable<IFormFile> files, int storageId, string path);
}

public class PhotoAdminService : IPhotoAdminService
{
    private readonly IPhotoIngestionService _photoIngestionService;

    public PhotoAdminService(IPhotoIngestionService photoIngestionService)
    {
        _photoIngestionService = photoIngestionService;
    }

    public Task UploadPhotosAsync(IEnumerable<IFormFile> files, int storageId, string path) =>
        _photoIngestionService.UploadAsync(files, storageId, path);
}
