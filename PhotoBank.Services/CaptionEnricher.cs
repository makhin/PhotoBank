using System.Collections.Generic;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using PhotoBank.DbContext.Models;

namespace PhotoBank.Services
{
    public class CaptionEnricher : IEnricher<ImageAnalysis>
    {
        public void Enrich(Photo photo, ImageAnalysis analysis)
        {
            photo.Captions = new List<Caption>();
            foreach (var caption in analysis.Description.Captions)
            {
                photo.Captions.Add(new Caption
                {
                    Confidence = caption.Confidence,
                    Text = caption.Text
                });
            }
        }
    }
}
