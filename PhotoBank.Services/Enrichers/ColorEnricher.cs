using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using PhotoBank.DbContext.Models;

namespace PhotoBank.Services.Enrichers
{
    public class ColorEnricher : IEnricher<ImageAnalysis>
    {
        public void Enrich(Photo photo, ImageAnalysis analysis)
        {
            photo.IsBW = analysis.Color.IsBWImg;
            photo.AccentColor = analysis.Color.AccentColor;
            photo.DominantColorBackground = analysis.Color.DominantColorBackground;
            photo.DominantColorForeground = analysis.Color.DominantColorForeground;
            photo.DominantColors = string.Join(",", analysis.Color.DominantColors);
        }
    }
}
