using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using PhotoBank.DbContext.Models;
using PhotoBank.Services.ImageAnalysis;
using PhotoBank.Services.Models;

namespace PhotoBank.Services.Enrichers;

public sealed class AnalyzeEnricher : IEnricher
{
    private readonly IImageAnalyzer _imageAnalyzer;

    private static readonly Type[] s_dependencies = [typeof(AdultEnricher)];

    public AnalyzeEnricher(IImageAnalyzer imageAnalyzer)
    {
        _imageAnalyzer = imageAnalyzer ?? throw new ArgumentNullException(nameof(imageAnalyzer));
    }

    public EnricherType EnricherType => EnricherType.Analyze;

    public Type[] Dependencies => s_dependencies;

    public async Task EnrichAsync(Photo photo, SourceDataDto source, CancellationToken cancellationToken = default)
    {
        if (source.PreviewImage is null)
            return;

        if (source.ImageAnalysis != null)
            return;

        using var stream = new MemoryStream(source.PreviewImage.ToByteArray());
        source.ImageAnalysis = await _imageAnalyzer.AnalyzeAsync(stream, cancellationToken).ConfigureAwait(false);
    }
}
