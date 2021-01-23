using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using PhotoBank.DbContext.Models;
using PhotoBank.Dto;
using PhotoBank.Dto.Load;
using Directory = MetadataExtractor.Directory;
using File = PhotoBank.DbContext.Models.File;

namespace PhotoBank.Services.Enrichers
{
    public class MetadataEnricher : IEnricher
    {
        public MetadataEnricher()
        {
        }
        public Type[] Dependencies => new Type[1] { typeof(PreviewEnricher) };

        public async Task Enrich(Photo photo, SourceDataDto sourceData)
        {
            await Task.Run(() =>
            {
                photo.Name = Path.GetFileNameWithoutExtension(sourceData.AbsolutePath);
                photo.RelativePath =
                    Path.GetDirectoryName(Path.GetRelativePath(photo.Storage.Folder, sourceData.AbsolutePath));
                photo.Files = new List<File>
                {
                    new File
                    {
                        Name = Path.GetFileName(sourceData.AbsolutePath)
                    }
                };

                IEnumerable<Directory> directories = ImageMetadataReader.ReadMetadata(sourceData.AbsolutePath);

                var exifIfd0Directory = directories.OfType<ExifIfd0Directory>().FirstOrDefault();
                var exifSubIfdDirectory = directories.OfType<ExifSubIfdDirectory>().FirstOrDefault();
                var gpsDirectory = directories.OfType<GpsDirectory>().FirstOrDefault();

                if (exifSubIfdDirectory != null || exifIfd0Directory != null)
                {
                    photo.TakenDate = GetTakenDate(new Directory[] { exifIfd0Directory, exifSubIfdDirectory});
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
            int[] tags = { ExifDirectoryBase.TagDateTime, ExifDirectoryBase.TagDateTimeOriginal };
            
            foreach (var directory in directories)
            {
                foreach (var tag in tags)
                {
                    try
                    {
                        return directory?.GetDateTime(tag);
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
            int[] tags = { ExifDirectoryBase.TagImageHeight, ExifDirectoryBase.TagExifImageHeight };

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
            int[] tags = { ExifDirectoryBase.TagImageWidth, ExifDirectoryBase.TagExifImageWidth };

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
