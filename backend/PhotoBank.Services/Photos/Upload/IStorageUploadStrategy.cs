using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using PhotoBank.DbContext.Models;

namespace PhotoBank.Services.Photos.Upload;

public interface IStorageUploadStrategy
{
    bool CanHandle(Storage storage);

    Task UploadAsync(
        Storage storage,
        IEnumerable<IFormFile> files,
        string? relativePath,
        CancellationToken cancellationToken);
}
