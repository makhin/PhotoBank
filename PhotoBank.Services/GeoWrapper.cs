using System.Collections.Generic;
using Amazon.Rekognition.Model;
using MetadataExtractor.Formats.Exif;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using NetTopologySuite;
using NetTopologySuite.Geometries;
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

        public static List<int?> GetRectangleArray(Geometry geometry)
        {
            return new List<int?>
            {
                0,
                0,
                (int?)(geometry.Coordinates[1].X - geometry.Coordinates[0].X),
                (int?)(geometry.Coordinates[3].Y - geometry.Coordinates[0].Y)
            };
        }

        public static Geometry GetRectangle(int imageHeight, int imageWidth, BoundingBox boundingBox, double scale = 1)
        {
            var x = (int)(imageWidth * boundingBox.Left / scale);
            var y = (int)(imageHeight * boundingBox.Top / scale);
            var w = (int)(imageWidth * boundingBox.Width / scale);
            var h = (int)(imageHeight * boundingBox.Height / scale);

            return GeometryFactory.CreatePolygon(
                new[]
                {
                    new Coordinate(x, y),
                    new Coordinate(x + h, y),
                    new Coordinate(x + h, y + w),
                    new Coordinate(x, y + w),
                    new Coordinate(x, y)
                });
        }

        public static Geometry GetRectangle(BoundingRect rectangle, double scale = 1)
        {
            var x = (int)(rectangle.X / scale);
            var y = (int)(rectangle.Y / scale);
            var w = (int)(rectangle.W / scale);
            var h = (int)(rectangle.H / scale);

            return GeometryFactory.CreatePolygon(
                new[]
                {
                    new Coordinate(x, y),
                    new Coordinate(x + h, y),
                    new Coordinate(x + h, y + w),
                    new Coordinate(x, y + w),
                    new Coordinate(x, y)
                });
        }
        
        public static Geometry GetRectangle(FaceRectangle rectangle, in double scale = 1)
        {
            int left = (int)(rectangle.Left / scale);
            int top = (int)(rectangle.Top / scale);
            int width = (int)(rectangle.Width / scale);
            int height = (int)(rectangle.Height / scale);

            return GeometryFactory.CreatePolygon(
                new[]
                {
                    new Coordinate(left, top),
                    new Coordinate(left + width, top),
                    new Coordinate(left + width, top + height),
                    new Coordinate(left, top + height),
                    new Coordinate(left, top),
                });
        }
        
        public static Point GetLocation(GpsDirectory gpsDirectory)
        {
            var location = gpsDirectory.GetGeoLocation();
            return location != null ? GeometryFactory.CreatePoint(new Coordinate(location.Latitude, location.Longitude)) : null;
        }

        public static Geometry GetRectangle(Microsoft.Azure.CognitiveServices.Vision.Face.Models.FaceRectangle rectangle, in double scale)
        {
            int left = (int)(rectangle.Left / scale);
            int top = (int)(rectangle.Top / scale);
            int width = (int)(rectangle.Width / scale);
            int height = (int)(rectangle.Height / scale);

            return GeometryFactory.CreatePolygon(
                new[]
                {
                    new Coordinate(left, top),
                    new Coordinate(left + width, top),
                    new Coordinate(left + width, top + height),
                    new Coordinate(left, top + height),
                    new Coordinate(left, top),
                });
        }
    }
}
