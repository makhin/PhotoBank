using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PhotoBank.DbContext.Models;
using PhotoBank.Services.Models;

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

        if (string.IsNullOrWhiteSpace(photo.RelativePath))
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
        var fileName = photo.Files.First().Name;
        return Path.Combine(storage.Folder, photo.RelativePath, fileName);
    }
}
