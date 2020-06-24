using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;

namespace PhotoBank.Services
{
    public interface IImageEncoder
    {
        Stream Encode(Image image, string mimeType, long encoderParameterValue);
    }

    public class ImageEncoder : IImageEncoder
    {
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
