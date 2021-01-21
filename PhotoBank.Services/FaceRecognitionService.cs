using System;
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
    }

    public class FaceRecognitionService : IFaceRecognitionService
    {
        private readonly FaceRecognition _faceRecognition;

        public FaceRecognitionService()
        {
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
    }

    public class FaceComparer : IEqualityComparer<Face>
    {
        public bool Equals(Face x, Face y)
        {
            FaceEncoding xEncoding = GetEncoding(x.Encoding);
            FaceEncoding yEncoding = GetEncoding(x.Encoding); ;

            return FaceRecognition.CompareFace(xEncoding, yEncoding, 0.65);
        }

        public int GetHashCode(Face face)
        {
            return face.Encoding.GetHashCode();
        }

        private static FaceEncoding GetEncoding(byte[] bytes)
        {
            var binaryFormatter = new BinaryFormatter();
            using (var memoryStream = new MemoryStream(bytes))
            {
                return (FaceEncoding)binaryFormatter.Deserialize(memoryStream);
            }
        }
    }
}
