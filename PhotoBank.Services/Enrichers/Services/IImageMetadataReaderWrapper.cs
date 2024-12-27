using System.Collections.Generic;
using Directory = MetadataExtractor.Directory;

namespace PhotoBank.Services.Enrichers.Services
{
    public interface IImageMetadataReaderWrapper
    {
        IEnumerable<Directory> ReadMetadata(string path);
    }
}
