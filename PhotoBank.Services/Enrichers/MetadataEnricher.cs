using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using MetadataExtractor.Formats.FileSystem;
using PhotoBank.DbContext.Models;
using PhotoBank.Dto.Load;
using Directory = MetadataExtractor.Directory;
using File = PhotoBank.DbContext.Models.File;

namespace PhotoBank.Services.Enrichers
{
    public class MetadataEnricher : IEnricher
    {
        public EnricherType EnricherType => EnricherType.Metadata;
        public Type[] Dependencies => new Type[1] { typeof(PreviewEnricher) };

        public async Task EnrichAsync(Photo photo, SourceDataDto sourceData)
        {
            await Task.Run(() =>
            {
                photo.Name = Path.GetFileNameWithoutExtension(sourceData.AbsolutePath);
                photo.RelativePath =
                    Path.GetDirectoryName(Path.GetRelativePath(photo.Storage.Folder, sourceData.AbsolutePath));
                photo.Files = new List<File>
                {
                    new()
                    {
                        Name = Path.GetFileName(sourceData.AbsolutePath)
                    }
                };

                IEnumerable<Directory> directories = ImageMetadataReader.ReadMetadata(sourceData.AbsolutePath);

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
            });
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

        private static int? GetHeight(Directory directory)
        {
            int[] tags = [ExifDirectoryBase.TagImageHeight, ExifDirectoryBase.TagExifImageHeight];

            foreach (var tag in tags)
            {
                try
                {
                    return directory?.GetInt32(tag);
                }
                catch (Exception)
                {
                    // ignored
                }
            }

            return null;
        }

        private static int? GetWidth(Directory directory)
        {
            int[] tags = [ExifDirectoryBase.TagImageWidth, ExifDirectoryBase.TagExifImageWidth];

            foreach (var tag in tags)
            {
                try
                {
                    return directory?.GetInt32(tag);
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
