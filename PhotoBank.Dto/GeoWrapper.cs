﻿using System.Collections.Generic;
using MetadataExtractor.Formats.Exif;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using NetTopologySuite;
using NetTopologySuite.Geometries;
using PhotoBank.Dto.View;

namespace PhotoBank.Dto
{
    public static class GeoWrapper
    {
        private static readonly GeometryFactory GeometryFactory;

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
    }
}
