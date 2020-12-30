using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using PhotoBank.DbContext.Models;
using PhotoBank.Dto;

namespace PhotoBank.Services.Enrichers
{
    public class AnalyzeEnricher : IEnricher
    {
        private readonly IComputerVisionClient _client;

        private readonly IList<VisualFeatureTypes?> _features = new List<VisualFeatureTypes?>()
        {
            VisualFeatureTypes.Categories, VisualFeatureTypes.Description,
            VisualFeatureTypes.Faces, VisualFeatureTypes.ImageType,
            VisualFeatureTypes.Tags, VisualFeatureTypes.Adult,
            VisualFeatureTypes.Color, VisualFeatureTypes.Brands,
            VisualFeatureTypes.Objects
        };

        public AnalyzeEnricher(IComputerVisionClient client)
        {
            _client = client;
        }

        public Type[] Dependencies => new Type[1] {typeof(PreviewEnricher)};

        public async Task Enrich(Photo photo, SourceDataDto source)
        {
            source.ImageAnalysis = await _client.AnalyzeImageInStreamAsync(new MemoryStream(photo.PreviewImage), _features);
        }
    }
}
