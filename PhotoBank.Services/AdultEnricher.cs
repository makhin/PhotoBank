using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using PhotoBank.DbContext.Models;

namespace PhotoBank.Services
{
    public class AdultEnricher : IEnricher<ImageAnalysis>
    {
        public void Enrich(Photo photo, ImageAnalysis analysis)
        {
            photo.IsAdultContent = analysis.Adult.IsAdultContent;
            photo.AdultScore = analysis.Adult.AdultScore;
            photo.IsRacyContent = analysis.Adult.IsRacyContent;
            photo.RacyScore = analysis.Adult.RacyScore;
        }
    }
}
