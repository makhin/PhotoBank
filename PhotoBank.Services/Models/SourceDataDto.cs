using ImageMagick;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using System.ComponentModel.DataAnnotations;

namespace PhotoBank.Services.Models
{
    public class SourceDataDto
    {
        [Required]
        public string AbsolutePath { get; set; }

        [Required]
        public ImageAnalysis ImageAnalysis { get; set; }
        [Required]
        public IMagickImage<byte> OriginalImage { get; set; }
        [Required]
        public IMagickImage<byte> PreviewImage { get; set; }
    }
}
