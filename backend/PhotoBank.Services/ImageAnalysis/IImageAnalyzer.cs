using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace PhotoBank.Services.ImageAnalysis;

public enum ImageAnalyzerKind { Azure, Ollama }

public interface IImageAnalyzer
{
    ImageAnalyzerKind Kind { get; }

    Task<ImageAnalysisResult> AnalyzeAsync(Stream image, CancellationToken ct = default);
}
