using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using MetadataExtractor.Formats.FileSystem;
using PhotoBank.DbContext.Models;
using PhotoBank.Services.Enrichers.Services;
using PhotoBank.Services.Models;
using Directory = MetadataExtractor.Directory;
using File = PhotoBank.DbContext.Models.File;

namespace PhotoBank.Services.Enrichers
{
    public class MetadataEnricher : IEnricher
    {
        private readonly IImageMetadataReaderWrapper _imageMetadataReaderWrapper;

        public MetadataEnricher(IImageMetadataReaderWrapper imageMetadataReaderWrapper)
        {
            _imageMetadataReaderWrapper = imageMetadataReaderWrapper;
        }

        public EnricherType EnricherType => EnricherType.Metadata;
        public Type[] Dependencies => new Type[1] { typeof(PreviewEnricher) };

        public Task EnrichAsync(Photo photo, SourceDataDto sourceData)
        {
            var normalizedAbsolutePath = sourceData.AbsolutePath.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);
            var normalizedStoragePath = photo.Storage.Folder.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);

            photo.Name = Path.GetFileNameWithoutExtension(normalizedAbsolutePath);
            photo.RelativePath = Path.GetDirectoryName(normalizedAbsolutePath)?
                .Replace(normalizedStoragePath, string.Empty)
                .TrimStart(Path.DirectorySeparatorChar);
            photo.Files = new List<File>
            {
                new()
                {
                    Name = Path.GetFileName(normalizedAbsolutePath)
                }
            };

            IEnumerable<Directory> directories = _imageMetadataReaderWrapper.ReadMetadata(sourceData.AbsolutePath);

            var exifIfd0Directory = directories.OfType<ExifIfd0Directory>().FirstOrDefault();
            var exifSubIfdDirectory = directories.OfType<ExifSubIfdDirectory>().FirstOrDefault();
            var fileMetadataDirectory = directories.OfType<FileMetadataDirectory>().FirstOrDefault();

            var gpsDirectory = directories.OfType<GpsDirectory>().FirstOrDefault();

            if (exifSubIfdDirectory != null || exifIfd0Directory != null || fileMetadataDirectory != null)
            {
                photo.TakenDate = GetTakenDate(new Directory[] { exifIfd0Directory, exifSubIfdDirectory, fileMetadataDirectory });
            }

            if (exifSubIfdDirectory != null)
            {
                photo.Height ??= GetHeight(exifSubIfdDirectory);
                photo.Width ??= GetWidth(exifSubIfdDirectory);
            }

            if (exifIfd0Directory != null)
            {
                photo.Orientation ??= GetOrientation(exifIfd0Directory);
            }

            if (gpsDirectory != null)
            {
                photo.Location = GeoWrapper.GetLocation(gpsDirectory);
            }

            return Task.CompletedTask;
        }

        private static DateTime? GetTakenDate(IEnumerable<Directory> directories)
        {
            int[] tags =
            [
                ExifDirectoryBase.TagDateTime, ExifDirectoryBase.TagDateTimeOriginal,
                    FileMetadataDirectory.TagFileModifiedDate
            ];

            foreach (var directory in directories.Where(d => d != null))
            {
                foreach (var tag in tags)
                {
                    try
                    {
                        return directory.GetDateTime(tag);
                    }
                    catch (Exception)
                    {
                        // ignored
                    }
                }
            }

            return null;
        }

        private static uint? GetHeight(Directory directory)
        {
            int[] tags = [ExifDirectoryBase.TagImageHeight, ExifDirectoryBase.TagExifImageHeight];

            foreach (var tag in tags)
            {
                try
                {
                    return (uint)directory?.GetInt32(tag);
                }
                catch (Exception)
                {
                    // ignored
                }
            }

            return null;
        }

        private static uint? GetWidth(Directory directory)
        {
            int[] tags = [ExifDirectoryBase.TagImageWidth, ExifDirectoryBase.TagExifImageWidth];

            foreach (var tag in tags)
            {
                try
                {
                    return (uint)directory?.GetInt32(tag);
                }
                catch (Exception)
                {
                    // ignored
                }
            }

            return null;
        }

        private static int? GetOrientation(Directory directory)
        {
            try
            {
                return directory?.GetInt32(ExifDirectoryBase.TagOrientation);
            }
            catch (Exception)
            {
                // ignored
            }

            return null;
        }
    }
}
