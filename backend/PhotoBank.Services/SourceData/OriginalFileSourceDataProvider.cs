using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PhotoBank.DbContext.Models;
using PhotoBank.Services.Models;
using File = System.IO.File;

namespace PhotoBank.Services.SourceData;

/// <summary>
/// Provides source data from the original photo file in storage
/// </summary>
public class OriginalFileSourceDataProvider : ISourceDataProvider
{
    public bool CanProvideData(Photo photo, Storage storage)
    {
        if (photo?.Files == null || !photo.Files.Any())
            return false;

        if (string.IsNullOrWhiteSpace(storage?.Folder))
            return false;

        // Find file for this storage (cross-storage support)
        var file = photo.Files.FirstOrDefault(f => f.StorageId == storage.Id);
        if (file == null || string.IsNullOrWhiteSpace(file.RelativePath))
            return false;

        var absolutePath = GetAbsolutePath(photo, storage);
        return File.Exists(absolutePath);
    }

    public Task<SourceDataDto> GetSourceDataAsync(Photo photo, Storage storage, CancellationToken cancellationToken = default)
    {
        var absolutePath = GetAbsolutePath(photo, storage);

        if (!File.Exists(absolutePath))
        {
            throw new FileNotFoundException($"Original file not found: {absolutePath}", absolutePath);
        }

        var sourceData = new SourceDataDto
        {
            AbsolutePath = absolutePath
        };

        return Task.FromResult(sourceData);
    }

    private string GetAbsolutePath(Photo photo, Storage storage)
    {
        // Get file from specific storage for cross-storage duplicate support
        var file = photo.Files.First(f => f.StorageId == storage.Id);
        return Path.Combine(storage.Folder, file.RelativePath ?? string.Empty, file.Name);
    }
}
