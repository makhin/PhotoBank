using System;
using System.Collections.Generic;
using ImageMagick;
using MetadataExtractor.Formats.Exif;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using NetTopologySuite;
using NetTopologySuite.Geometries;
using PhotoBank.Dto.View;
using Location = FaceRecognitionDotNet.Location;

namespace PhotoBank.Dto
{
    public static class GeoWrapper
    {
        private static readonly GeometryFactory GeometryFactory;
        private const int MinFaceSize = 36;
        public const int MaxSize = 1920;

        static GeoWrapper()
        {
            GeometryFactory = NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);
        }

        public static FaceBoxDto GetFaceBox(Geometry geometry, double scale = 1)
        {
            const string suffix = "px";
            return new FaceBoxDto
            {
                Left  = (int)(geometry.Coordinates[0].X * scale) + suffix,
                Top = (int)(geometry.Coordinates[0].Y * scale) + suffix,
                Width = (int)((geometry.Coordinates[1].X - geometry.Coordinates[0].X) * scale) + suffix,
                Height = (int)((geometry.Coordinates[3].Y - geometry.Coordinates[0].Y) * scale) + suffix
            };
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

        public static bool IsEnoughSize(this Location faceLocation)
        {
            return faceLocation.Bottom - faceLocation.Top >= MinFaceSize &&
                faceLocation.Right - faceLocation.Left >= MinFaceSize;
        }

        public static Geometry GetRectangle(Location location)
        {
            int left = location.Left;
            int top = location.Top;
            int right = location.Right;
            int bottom = location.Bottom;

            return GeometryFactory.CreatePolygon(
                new[]
                {
                    new Coordinate(left, top),
                    new Coordinate(right, top),
                    new Coordinate(right, bottom),
                    new Coordinate(left, bottom),
                    new Coordinate(left, top),
                });
        }

        public static Geometry GetRectangle(FaceRectangle rectangle, double scale = 1)
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

        public static void ResizeImage(IMagickImage<byte> image, out double scale)
        {
            var isLandscape = image.Width > image.Height;
            var maxSize = isLandscape ? image.Width : image.Height;
            scale = 1;

            if (maxSize <= MaxSize) return;

            if (isLandscape)
            {
                scale = ((double)MaxSize / image.Width);
                var geometry = new MagickGeometry(MaxSize, (int)scale * image.Height);
                image.Resize(geometry);
            }
            else
            {
                scale = ((double)MaxSize / image.Height);
                var geometry = new MagickGeometry((int)scale * image.Width, MaxSize);
                image.Resize(geometry);
            }
        }

        public static MagickGeometry GetMagickGeometry(Location location, double margin = 0)
        {
            var originalWidth = location.Right - location.Left;
            var originalHeight = location.Bottom - location.Top;

            int width = (int)Math.Round(originalWidth * (100 + 2 * margin) / 100);
            int height = (int)Math.Round(originalHeight * (100 + 2 * margin) / 100);

            var geometry = new MagickGeometry(width, height)
            {
                IgnoreAspectRatio = true,
                Y = (int)Math.Round(location.Top - originalHeight * margin / 100),
                X = (int)Math.Round(location.Left - originalWidth * margin / 100 )
            };
            return geometry;
        }
    }
}
