using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;
using FaceRecognitionDotNet;
using FaceRecognitionDotNet.Extensions;
using ImageMagick;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using PhotoBank.DbContext.Models;
using PhotoBank.Dto;
using PhotoBank.Dto.Load;
using ImageFormat = System.Drawing.Imaging.ImageFormat;

namespace PhotoBank.Services.Enrichers
{
    public class FaceEnricher : IEnricher
    {
        private readonly IFaceService _faceService;
        private readonly FaceRecognition _faceRecognition;
        //private SimpleGenderEstimator _genderEstimator;
        //private SimpleAgeEstimator _ageEstimator;

        public FaceEnricher(IFaceService faceService)
        {
            _faceService = faceService;
            const string directory = @"C:\Temp\HelenTraining\Models";
            _faceRecognition = FaceRecognition.Create(directory);
        }

        public Type[] Dependencies => new[] { typeof(AnalyzeEnricher), typeof(MetadataEnricher) };

        public async Task Enrich(Photo photo, SourceDataDto sourceData)
        {

            using (var magickImage = new MagickImage(sourceData.AbsolutePath, MagickFormat.Jpg))
            {
                magickImage.AutoOrient();

                var image = FaceRecognition.LoadImage(magickImage.ToBitmap(ImageFormat.Jpeg));
                var faceLocations = _faceRecognition.FaceLocations(image).ToList();
                if (faceLocations.Count > 0)
                {
                    photo.Faces = new List<Face>();  //TODO check sizes
                }

                var faceEncodings = _faceRecognition.FaceEncodings(image, faceLocations, 1, PredictorModel.Large).ToList();

                for (var i = 0; i < faceLocations.Count; i++)
                {
                    var faceLocation = faceLocations[i];


                    var faceImage = magickImage.Clone();

                    faceImage.Crop(GeoWrapper.GetMagickGeometry(faceLocation, 5));
                    GeoWrapper.ResizeImage(faceImage, out _);

                    using (var stream = new MemoryStream())
                    {
                        await faceImage.WriteAsync(stream);

                        var face = new Face
                        {
                            Rectangle = GeoWrapper.GetRectangle(faceLocation),
                            Image = stream.ToArray(),
                            Encoding = GetEncoding(faceEncodings[i]),
                            Photo = photo
                        };

                        photo.Faces.Add(face);

                        await _faceService.FaceIdentityAsync(face);
                    }
                }
            }
        }

        private static byte[] GetEncoding(FaceEncoding encoding)
        {
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
