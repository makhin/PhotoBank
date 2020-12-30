using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PhotoBank.DbContext.Models;
using PhotoBank.Dto;

namespace PhotoBank.Services.Enrichers
{
    public class FaceEnricher : IEnricher
    {
        private readonly IGeoWrapper _geoWrapper;
        private const int MinFaceSize = 36;

        public FaceEnricher(IGeoWrapper geoWrapper)
        {
            _geoWrapper = geoWrapper;
        }

        public Type[] Dependencies => new Type[1] { typeof(AnalyzeEnricher) };

        public async Task Enrich(Photo photo, SourceDataDto sourceData)
        {
            await Task.Run(async () =>
            {

                if (!sourceData.ImageAnalysis.Faces.Any())
                {
                    return;
                }

                photo.Faces = new List<Face>();
                foreach (var faceDescription in sourceData.ImageAnalysis.Faces)
                {

                    if (faceDescription.FaceRectangle.Height / photo.Scale < MinFaceSize ||
                        faceDescription.FaceRectangle.Width / photo.Scale < MinFaceSize)
                    {
                        continue;
                    }

                    var image = await ImageHelper.GetFace(sourceData.AbsolutePath, photo.Scale, faceDescription.FaceRectangle);

                    photo.Faces.Add(new Face()
                    {
                        Age = faceDescription.Age,
                        Rectangle = _geoWrapper.GetRectangle(faceDescription.FaceRectangle, photo.Scale),
                        Gender = faceDescription.Gender.HasValue ? (int) faceDescription.Gender.Value : (int?) null,
                        Image = image
                    });
                }
            });
        }
    }
}
