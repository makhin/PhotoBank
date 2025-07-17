using System.Threading.Tasks;
using ImageMagick;
using Microsoft.Azure.CognitiveServices.Vision.Face.Models;

namespace PhotoBank.Services.Enrichers.Services
{
    public interface IFacePreviewService
    {
        Task<byte[]> CreateFacePreview(DetectedFace detectedFace, IMagickImage<byte> image, double photoScale);
    }
}
