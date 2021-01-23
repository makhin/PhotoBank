using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;

namespace PhotoBank.Dto.Load
{
    public class SourceDataDto
    {
        public string AbsolutePath { get; set; }

        public ImageAnalysis ImageAnalysis { get; set; }
    }
}
