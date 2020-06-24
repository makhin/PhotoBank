using System.Drawing;
using System.IO;
using PhotoBank.DbContext.Models;

namespace PhotoBank.Services
{
    public class PreviewEnricher : IEnricher<string>
    {
        private readonly IImageEncoder _imageEncoder;

        public PreviewEnricher(IImageEncoder imageEncoder)
        {
            _imageEncoder = imageEncoder;
        }
        public void Enrich(Photo photo, string path)
        {
            var image = Image.FromFile(path);
            var stream = _imageEncoder.Encode(image, @"image/jpeg", 60L);
            stream.Position = 0;

            var binReader = new BinaryReader(stream);
            var arraySize = (int)(stream.Length - stream.Position);
            photo.PreviewImage = new byte[arraySize];
            binReader.Read(photo.PreviewImage, 0, arraySize);
        }
    }
}
