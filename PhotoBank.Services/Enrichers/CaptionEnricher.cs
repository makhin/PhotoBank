using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PhotoBank.DbContext.Models;
using PhotoBank.Dto.Load;

namespace PhotoBank.Services.Enrichers
{
    public class CaptionEnricher : IEnricher
    {
        public Type[] Dependencies => new Type[1] { typeof(AnalyzeEnricher) };

        public async Task Enrich(Photo photo, SourceDataDto sourceData)
        {
            await Task.Run(() =>
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
            });
        }
    }
}
