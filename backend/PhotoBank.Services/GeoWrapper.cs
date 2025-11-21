using System.Collections.Generic;
using Amazon.Rekognition.Model;
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using NetTopologySuite;
using NetTopologySuite.Geometries;
using PhotoBank.Services.FaceRecognition.Abstractions;
using PhotoBank.Services.ImageAnalysis;
using Geometry = NetTopologySuite.Geometries.Geometry;
using Point = NetTopologySuite.Geometries.Point;

namespace PhotoBank.Services
{
    public static class GeoWrapper
    {
        private static readonly GeometryFactory GeometryFactory;

        static GeoWrapper()
        {
            GeometryFactory = NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);
        }

        /// <summary>
        /// Создает прямоугольный полигон по координатам и размерам
        /// </summary>
        private static Geometry CreateRectanglePolygon(int x, int y, int width, int height)
        {
            return GeometryFactory.CreatePolygon(
            [
                new Coordinate(x, y),
                new Coordinate(x + width, y),
                new Coordinate(x + width, y + height),
                new Coordinate(x, y + height),
                new Coordinate(x, y)
            ]);
        }

        public static List<int?> GetRectangleArray(Geometry geometry)
        {
            return
            [
                0,
                0,
                (int?) (geometry.Coordinates[1].X - geometry.Coordinates[0].X),
                (int?) (geometry.Coordinates[3].Y - geometry.Coordinates[0].Y)
            ];
        }

        public static Geometry GetRectangle(uint imageHeight, uint imageWidth, BoundingBox boundingBox, double scale = 1)
        {
            var x = (int)(imageWidth * boundingBox.Left / scale);
            var y = (int)(imageHeight * boundingBox.Top / scale);
            var width = (int)(imageWidth * boundingBox.Width / scale);
            var height = (int)(imageHeight * boundingBox.Height / scale);

            return CreateRectanglePolygon(x, y, width, height);
        }

        public static Geometry GetRectangle(BoundingRect rectangle, double scale = 1)
        {
            var x = (int)(rectangle.X / scale);
            var y = (int)(rectangle.Y / scale);
            var width = (int)(rectangle.W / scale);
            var height = (int)(rectangle.H / scale);

            return CreateRectanglePolygon(x, y, width, height);
        }

        public static Geometry GetRectangle(ObjectRectangle rectangle, double scale = 1)
        {
            var x = (int)(rectangle.X / scale);
            var y = (int)(rectangle.Y / scale);
            var width = (int)(rectangle.W / scale);
            var height = (int)(rectangle.H / scale);

            return CreateRectanglePolygon(x, y, width, height);
        }

        public static Geometry GetRectangle(FaceRectangle rectangle, in double scale = 1)
        {
            var left = (int)(rectangle.Left / scale);
            var top = (int)(rectangle.Top / scale);
            var width = (int)(rectangle.Width / scale);
            var height = (int)(rectangle.Height / scale);

            return CreateRectanglePolygon(left, top, width, height);
        }

        public static Point? GetLocation(GpsDirectory gpsDirectory)
        {
            if (gpsDirectory.TryGetGeoLocation(out var location))
                return GeometryFactory.CreatePoint(new Coordinate(location.Latitude, location.Longitude));
            return null;
        }

        public static Geometry GetRectangle(Microsoft.Azure.CognitiveServices.Vision.Face.Models.FaceRectangle rectangle, in double scale = 1)
        {
            var left = (int)(rectangle.Left / scale);
            var top = (int)(rectangle.Top / scale);
            var width = (int)(rectangle.Width / scale);
            var height = (int)(rectangle.Height / scale);

            return CreateRectanglePolygon(left, top, width, height);
        }

        /// <summary>
        /// Creates a rectangle geometry from unified FaceBoundingBox (normalized coordinates 0-1).
        /// </summary>
        /// <param name="boundingBox">Normalized bounding box (values 0.0 to 1.0)</param>
        /// <param name="imageWidth">Image width in pixels</param>
        /// <param name="imageHeight">Image height in pixels</param>
        /// <param name="scale">Scale factor to apply</param>
        /// <returns>Rectangle geometry</returns>
        public static Geometry GetRectangle(FaceBoundingBox boundingBox, uint imageWidth, uint imageHeight, double scale = 1)
        {
            var x = (int)(imageWidth * boundingBox.Left / scale);
            var y = (int)(imageHeight * boundingBox.Top / scale);
            var width = (int)(imageWidth * boundingBox.Width / scale);
            var height = (int)(imageHeight * boundingBox.Height / scale);

            return CreateRectanglePolygon(x, y, width, height);
        }

        /// <summary>
        /// Creates a rectangle geometry from ONNX object detection coordinates
        /// </summary>
        /// <param name="x">X coordinate (top-left corner)</param>
        /// <param name="y">Y coordinate (top-left corner)</param>
        /// <param name="width">Width of the rectangle</param>
        /// <param name="height">Height of the rectangle</param>
        /// <returns>Rectangle geometry</returns>
        public static Geometry CreateRectangleFromOnnxDetection(int x, int y, int width, int height)
        {
            return CreateRectanglePolygon(x, y, width, height);
        }
    }
}
