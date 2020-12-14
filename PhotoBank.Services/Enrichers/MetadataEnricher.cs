using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using PhotoBank.DbContext.Models;
using PhotoBank.Dto;
using Directory = MetadataExtractor.Directory;

namespace PhotoBank.Services.Enrichers
{
    public class MetadataEnricher : IEnricher
    {
        private readonly IGeoWrapper _geoWrapper;

        public MetadataEnricher(IGeoWrapper geoWrapper)
        {
            _geoWrapper = geoWrapper;
        }
        public Type[] Dependencies => new Type[0];

        public void Enrich(Photo photo, SourceDataDto sourceData)
        {
            photo.Name = Path.GetFileNameWithoutExtension(sourceData.Path);
            photo.Path = Path.GetDirectoryName(sourceData.Path);
            IEnumerable<Directory> directories = ImageMetadataReader.ReadMetadata(Path.Combine(photo.Storage.Folder, sourceData.Path));

            var exifSubIfdDirectory = directories.OfType<ExifSubIfdDirectory>().FirstOrDefault();
            if (exifSubIfdDirectory != null)
            {
                photo.TakenDate = GetTakenDate(exifSubIfdDirectory);
            }

            var exifIfd0Directory = directories.OfType<ExifIfd0Directory>().FirstOrDefault();
            if (exifIfd0Directory != null)
            {
                photo.Height = GetHeight(exifSubIfdDirectory);
                photo.Width = GetWidth(exifSubIfdDirectory);
                photo.Orientation = GetOrientation(exifIfd0Directory);
            }

            var gpsDirectory = directories.OfType<GpsDirectory>().FirstOrDefault();
            if (gpsDirectory != null)
            {
                photo.Location = _geoWrapper.GetLocation(gpsDirectory);
            }
        }

        private static DateTime? GetTakenDate(ExifSubIfdDirectory subIfdDirectory)
        {
            return subIfdDirectory?.GetDateTime(ExifDirectoryBase.TagDateTimeOriginal);
        }

        private static int? GetHeight(ExifSubIfdDirectory subIfdDirectory)
        {
            return subIfdDirectory?.GetInt32(ExifDirectoryBase.TagExifImageHeight);
        }

        private static int? GetWidth(ExifSubIfdDirectory subIfdDirectory)
        {
            return subIfdDirectory?.GetInt32(ExifDirectoryBase.TagExifImageWidth);
        }

        private static int? GetOrientation(ExifIfd0Directory exifIfd0Directory)
        {
            return exifIfd0Directory?.GetInt32(ExifDirectoryBase.TagOrientation);
        }
    }
}
