using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace PhotoBank.Services.Photos.Upload;

public sealed class UploadNameResolver
{
    public async Task<UploadResolution> ResolveAsync(
        string fileName,
        long fileLength,
        Func<string, Task<StoredObjectInfo?>> tryGetExistingAsync,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentNullException.ThrowIfNull(tryGetExistingAsync);

        var baseName = Path.GetFileNameWithoutExtension(fileName);
        var extension = Path.GetExtension(fileName);

        var index = 1;
        var candidate = fileName;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var existing = await tryGetExistingAsync(candidate).ConfigureAwait(false);
            if (existing == null)
            {
                return UploadResolution.Create(candidate);
            }

            if (existing.Length == fileLength)
            {
                return UploadResolution.Skip(candidate);
            }

            candidate = string.IsNullOrEmpty(extension)
                ? $"{baseName}_{index}"
                : $"{baseName}_{index}{extension}";
            index++;
        }
    }

    public sealed record StoredObjectInfo(long Length);

    public sealed record UploadResolution(bool ShouldUpload, string TargetName)
    {
        public static UploadResolution Skip(string name) => new(false, name);

        public static UploadResolution Create(string name) => new(true, name);
    }
}
