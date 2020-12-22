using System.Collections.Generic;
using MetadataExtractor.Formats.Exif;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using NetTopologySuite;
using NetTopologySuite.Geometries;

namespace PhotoBank.Services
{
    public interface IGeoWrapper
    {
        List<int?> GetRectangleArray(Geometry geometry);
        Geometry GetRectangle(BoundingRect rectangle, double scale = 1);
        Geometry GetRectangle(FaceRectangle rectangle, double scale = 1);
        Point GetLocation(GpsDirectory gpsDirectory);
    }

    public class GeoWrapper : IGeoWrapper
    {
        private readonly GeometryFactory _geometryFactory;

        public GeoWrapper()
        {
            _geometryFactory = NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);
        }

        public List<int?> GetRectangleArray(Geometry geometry)
        {
            return new List<int?>
            {
                0,
                0,
                (int?)(geometry.Coordinates[1].X - geometry.Coordinates[0].X),
                (int?)(geometry.Coordinates[3].Y - geometry.Coordinates[0].Y)
            };
        }

        public Geometry GetRectangle(BoundingRect rectangle, double scale = 1)
        {
            int x = (int)(rectangle.X / scale);
            int y = (int)(rectangle.Y / scale);
            int w = (int)(rectangle.W / scale);
            int h = (int)(rectangle.H / scale);

            return _geometryFactory.CreatePolygon(
                new[]
                {
                    new Coordinate(x, y),
                    new Coordinate(x + h, y),
                    new Coordinate(x + h, y + w),
                    new Coordinate(x, y + w),
                    new Coordinate(x, y)
                });
        }

        public Geometry GetRectangle(FaceRectangle rectangle, double scale = 1)
        {

            int left = (int)(rectangle.Left / scale);
            int top = (int)(rectangle.Top / scale);
            int width = (int)(rectangle.Width / scale);
            int height = (int)(rectangle.Height / scale);

            return _geometryFactory.CreatePolygon(
                new[]
                {
                    new Coordinate(left, top),
                    new Coordinate(left + width, top),
                    new Coordinate(left + width, top + height),
                    new Coordinate(left, top + height),
                    new Coordinate(left, top),
                });
        }

        public Point GetLocation(GpsDirectory gpsDirectory)
        {
            var location = gpsDirectory.GetGeoLocation();
            return location != null ? _geometryFactory.CreatePoint(new Coordinate(location.Latitude, location.Longitude)) : null;
        }
    }
}
