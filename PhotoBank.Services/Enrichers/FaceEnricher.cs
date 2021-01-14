using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;
using FaceRecognitionDotNet;
using ImageMagick;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using PhotoBank.DbContext.Models;
using PhotoBank.Dto;
using ImageFormat = System.Drawing.Imaging.ImageFormat;

namespace PhotoBank.Services.Enrichers
{
    public class FaceEnricher : IEnricher
    {
        private readonly IGeoWrapper _geoWrapper;
        private readonly FaceRecognition _faceRecognition;
        private const int MinFaceSize = 36;

        public FaceEnricher(IGeoWrapper geoWrapper)
        {
            _geoWrapper = geoWrapper;
            string directory = @"C:\Temp\HelenTraining\Models";
            _faceRecognition = FaceRecognition.Create(directory);
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

                    using (var magickImage = new MagickImage(sourceData.AbsolutePath, MagickFormat.Jpg))
                    {
                        magickImage.AutoOrient();
                        magickImage.Crop(GetMagickGeometry(photo, faceDescription));
                        ImageHelper.ResizeImage(magickImage, out _);

                        using (var stream = new MemoryStream())
                        {
                            await magickImage.WriteAsync(stream);

                            photo.Faces.Add(new Face
                            {
                                Age = faceDescription.Age,
                                Rectangle = _geoWrapper.GetRectangle(faceDescription.FaceRectangle, photo.Scale),
                                Gender = faceDescription.Gender.HasValue ? (int)faceDescription.Gender.Value : (int?)null,
                                Image = stream.ToArray(),
                                Encoding = GetEncoding(magickImage)
                            });

                        }
                    }
                }
            });
        }

        private static MagickGeometry GetMagickGeometry(Photo photo, FaceDescription faceDescription)
        {
            var geometry = new MagickGeometry((int) (faceDescription.FaceRectangle.Width / photo.Scale),
                (int) (faceDescription.FaceRectangle.Height / photo.Scale))
            {
                IgnoreAspectRatio = true,
                Y = (int) (faceDescription.FaceRectangle.Top / photo.Scale),
                X = (int) (faceDescription.FaceRectangle.Left / photo.Scale)
            };
            return geometry;
        }

        public byte[] GetEncoding(MagickImage magickImage)
        {
            using (var frImage = FaceRecognition.LoadImage(magickImage.ToBitmap(ImageFormat.Jpeg)))
            {
                var encoding = _faceRecognition.FaceEncodings(frImage,

                    new List<Location>
                    {
                        new Location(0, 0, magickImage.Width, magickImage.Height)
                    }, 1, PredictorModel.Large).FirstOrDefault();

                if (encoding == null)
                {
                    return null;
                }

                var binaryFormatter = new BinaryFormatter();
                using (var memoryStream = new MemoryStream())
                {
                    binaryFormatter.Serialize(memoryStream, encoding);
                    memoryStream.Flush();
                    return memoryStream.ToArray();
                }
            }
        }
    }
}
