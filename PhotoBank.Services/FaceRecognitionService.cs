using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using FaceRecognitionDotNet;
using ImageMagick;
using PhotoBank.DbContext.Models;
using PhotoBank.Repositories;
using ImageFormat = System.Drawing.Imaging.ImageFormat;

namespace PhotoBank.Services
{
    public interface IFaceRecognitionService
    {
        byte[] FaceEncode(byte[] image);
        void Test();
    }

    public class FaceRecognitionService : IFaceRecognitionService
    {
        private readonly IRepository<Face> _faceRepository;
        private readonly FaceRecognition _faceRecognition;

        public FaceRecognitionService(IRepository<Face> faceRepository)
        {
            _faceRepository = faceRepository;
            string directory = @"C:\Temp\HelenTraining\Models";
            _faceRecognition = FaceRecognition.Create(directory);
        }

        public byte[] FaceEncode(byte[] image)
        {
            var magickImage = new MagickImage(new MemoryStream(image), MagickFormat.Jpg);
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

        public void Test()
        {
            var imageB = _faceRepository.Get(1593, f => f).Image;
            var magickImageB = new MagickImage(new MemoryStream(imageB), MagickFormat.Jpg);

            var imageA = _faceRepository.Get(1594, f => f).Image;
            var magickImageA = new MagickImage(new MemoryStream(imageA), MagickFormat.Jpg);

            FaceEncoding encodingA;
            FaceEncoding encodingB;

            using (var frImageB = FaceRecognition.LoadImage(magickImageB.ToBitmap(ImageFormat.Jpeg)))
            {
                using (var frImageA = FaceRecognition.LoadImage(magickImageA.ToBitmap(ImageFormat.Jpeg)))
                {
                    encodingA = _faceRecognition.FaceEncodings(frImageA,
                        new List<Location> { new Location(0, 0, magickImageA.Width, magickImageA.Height) }, 1, PredictorModel.Large).First();
                    encodingB = _faceRecognition.FaceEncodings(frImageB,
                        new List<Location> { new Location(0, 0, magickImageB.Width, magickImageB.Height) }, 1, PredictorModel.Large).First();
                }
            }
            const double tolerance = 0.6d;
            bool match = FaceRecognition.CompareFace(encodingA, encodingB, tolerance);

            var binaryFormatter = new BinaryFormatter();
            using (var memoryStream = new MemoryStream())
            {
                binaryFormatter.Serialize(memoryStream, encodingA);
                memoryStream.Flush();

                byte[] array = memoryStream.ToArray();
                using (var ms2 = new MemoryStream(array))
                {
                    var de1 = binaryFormatter.Deserialize(ms2) as FaceEncoding;
                }
            }
        }
    }
}
