using System;
using System.Collections.Generic;
using PhotoBank.DbContext.Models;
using PhotoBank.Dto;

namespace PhotoBank.Services.Enrichers
{
    public class CaptionEnricher : IEnricher
    {
        public Type[] Dependencies => new Type[0];

        public void Enrich(Photo photo, SourceDataDto sourceData)

        {
            photo.Captions = new List<Caption>();
            foreach (var caption in sourceData.ImageAnalysis.Description.Captions)
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
