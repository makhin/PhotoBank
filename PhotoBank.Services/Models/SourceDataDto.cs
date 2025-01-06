using ImageMagick;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;

namespace PhotoBank.Services.Models
{
    public class SourceDataDto
    {
        public string AbsolutePath { get; set; }

        public ImageAnalysis ImageAnalysis { get; set; }
        public IMagickImage<byte> OriginalImage { get; set; }
        public IMagickImage<byte> PreviewImage { get; set; }
    }
}
