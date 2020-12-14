using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using ImageMagick;

namespace PhotoBank.Services
{
    public interface IImageEncoder
    {
        Stream Encode(Image image, string mimeType, long encoderParameterValue);

        byte[] Prepare(string path);
    }

    public class ImageEncoder : IImageEncoder
    {
        public const int MaxSize = 1920;

        public byte[] Prepare(string path)
        {
            var stream = new MemoryStream();
            using (var image = new MagickImage(path))
            {
                var maxSize = image.Width > image.Height ? image.Width : image.Height;
                var percentage = ((double)MaxSize / (double)maxSize) * 100.0;

                if (percentage < 100.0)
                {
                    image.Resize(new Percentage(percentage));
                }

                image.Format = MagickFormat.Jpg;
                image.Write(stream);
            }

            return stream.ToArray();
        }

        public Stream Encode(Image image, string mimeType, long encoderParameterValue)
        {
            Stream stream = new MemoryStream();
            var codecInfo = ImageCodecInfo.GetImageEncoders().FirstOrDefault(e => e.MimeType == mimeType);

            var encoder = Encoder.Quality;
            var encoderParams = new EncoderParameters(1);
            var encoderParameter = new EncoderParameter(encoder, encoderParameterValue);
            encoderParams.Param[0] = encoderParameter;

            image.Save(stream, codecInfo, encoderParams);
            return stream;
        }
    }
}
