using System.Collections.Generic;
using MetadataExtractor;
using Directory = MetadataExtractor.Directory;

namespace PhotoBank.Services.Enrichers.Services
{
    public class ImageMetadataReaderWrapper : IImageMetadataReaderWrapper
    {
        public IEnumerable<Directory> ReadMetadata(string path)
        {
            return ImageMetadataReader.ReadMetadata(path);
        }
    }
}
