using System;
using System.Drawing;
using System.IO;
using PhotoBank.DbContext.Models;
using PhotoBank.Dto;

namespace PhotoBank.Services.Enrichers
{
    public class PreviewEnricher : IEnricher
    {
        public Type[] Dependencies => new Type[0];

        public void Enrich(Photo photo, SourceDataDto sourceData)
        {
            photo.PreviewImage = ImageToByteArray(sourceData.Image);
            photo.Thumbnail = ImageToByteArray(sourceData.Image.GetThumbnailImage(120, 120, () => false, IntPtr.Zero));
        }

        private byte[] ImageToByteArray(Image image)
        {
            using (var ms = new MemoryStream())
            {
                image.Save(ms, image.RawFormat);
                return ms.ToArray();
            }
        }
    }
}
