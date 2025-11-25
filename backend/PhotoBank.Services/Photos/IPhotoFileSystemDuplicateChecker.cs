using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PhotoBank.DbContext.Models;
using PhotoBank.Repositories;
using File = PhotoBank.DbContext.Models.File;

namespace PhotoBank.Services.Photos;

public interface IPhotoFileSystemDuplicateChecker
{
    Task<DuplicateVerification> VerifyDuplicatesAsync(Storage storage, string path);
}

public sealed class PhotoFileSystemDuplicateChecker : IPhotoFileSystemDuplicateChecker
{
    private readonly IRepository<Photo> _photoRepository;
    private readonly IRepository<File> _fileRepository;

    public PhotoFileSystemDuplicateChecker(
        IRepository<Photo> photoRepository,
        IRepository<File> fileRepository)
    {
        _photoRepository = photoRepository;
        _fileRepository = fileRepository;
    }

    public async Task<DuplicateVerification> VerifyDuplicatesAsync(Storage storage, string path)
    {
        var name = Path.GetFileNameWithoutExtension(path);
        var directoryName = Path.GetDirectoryName(path);
        // path is already relative to storage.Folder, so directoryName is the relative path
        var relativePath = string.IsNullOrEmpty(directoryName)
            ? string.Empty
            : directoryName;

        // Convert "." to empty string for files in root directory
        if (relativePath == ".")
        {
            relativePath = string.Empty;
        }

        var result = new DuplicateVerification
        {
            PhotoId = await _photoRepository.GetByCondition(p =>
                    p.Name == name && p.RelativePath == relativePath && p.Storage.Id == storage.Id)
                .Select(p => p.Id)
                .SingleOrDefaultAsync(),
            Name = Path.GetFileName(path)
        };

        if (result.PhotoId == 0)
        {
            result.DuplicateStatus = DuplicateStatus.PhotoNotExists;
            return result;
        }

        var fileExists = await _fileRepository
            .GetByCondition(f => f.Name == result.Name && f.Photo.Id == result.PhotoId)
            .AnyAsync();
        result.DuplicateStatus = fileExists ? DuplicateStatus.FileExists : DuplicateStatus.FileNotExists;
        return result;
    }
}

public class DuplicateVerification
{
    public DuplicateStatus DuplicateStatus { get; set; }
    public int PhotoId { get; init; }
    public string Name { get; init; } = string.Empty;
}

public enum DuplicateStatus
{
    PhotoNotExists,
    FileNotExists,
    FileExists
}
