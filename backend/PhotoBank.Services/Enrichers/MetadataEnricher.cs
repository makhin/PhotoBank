using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
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
        // Now depends on DuplicateEnricher which handles Name, RelativePath, Files creation
        public Type[] Dependencies => [typeof(DuplicateEnricher)];

        public Task EnrichAsync(Photo photo, SourceDataDto sourceData, CancellationToken cancellationToken = default)
        {
            // Name, RelativePath, and Files creation moved to DuplicateEnricher
            // This enricher now only handles EXIF metadata extraction

            IEnumerable<Directory> directories = _imageMetadataReaderWrapper.ReadMetadata(sourceData.AbsolutePath);

            var exifIfd0Directory = directories.OfType<ExifIfd0Directory>().FirstOrDefault();
            var exifSubIfdDirectory = directories.OfType<ExifSubIfdDirectory>().FirstOrDefault();
            var fileMetadataDirectory = directories.OfType<FileMetadataDirectory>().FirstOrDefault();

            var gpsDirectory = directories.OfType<GpsDirectory>().FirstOrDefault();

            if (exifSubIfdDirectory != null || exifIfd0Directory != null || fileMetadataDirectory != null)
            {
                photo.TakenDate = GetTakenDate([exifIfd0Directory, exifSubIfdDirectory, fileMetadataDirectory]);
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
    }
}
